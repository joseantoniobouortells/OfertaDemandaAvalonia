<#
create-windows-msix.ps1
Builds and packages the Avalonia desktop app into an MSIX for Windows 11.

Usage (PowerShell):
  powershell -ExecutionPolicy Bypass -File .\scripts\create-windows-msix.ps1

Optional signing:
  $env:SIGN_CERT_PFX="C:\path\to\cert.pfx"
  $env:SIGN_CERT_PASSWORD="pfx-password"
#>

[CmdletBinding()]
param(
  [string]$Project = "",

  [ValidateSet("win-x64","win-arm64")]
  [string]$Rid = "win-x64",

  [ValidateSet("Release","Debug")]
  [string]$Config = "Release",

  [string]$PackageName = "OfertaDemandaAvalonia",
  [string]$IdentityName = "com.joseantoniobouortells.ofertademandaavalonia",
  [string]$Publisher = "CN=OfertaDemandaAvalonia Dev",
  [string]$PublisherDisplayName = "OfertaDemandaAvalonia Dev",
  [string]$Version = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Die($msg) { throw $msg }

function RepoRoot {
  return (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function FindProject($root) {
  if ($Project -and (Test-Path $Project)) { return (Resolve-Path $Project).Path }

  $candidate = Join-Path $root "src\OfertaDemanda.Desktop\OfertaDemanda.Desktop.csproj"
  if (Test-Path $candidate) { return $candidate }

  $found = Get-ChildItem -Path (Join-Path $root "src") -Recurse -Filter *.csproj |
    Where-Object { $_.FullName -match '(?i)(desktop|avalonia)' } |
    Select-Object -First 1

  if (-not $found) { Die "No pude autodetectar el .csproj. Usa -Project <ruta>." }
  return $found.FullName
}

function EnsureDotnet {
  if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Die "dotnet no esta instalado o no esta en PATH."
  }
}

function FindSdkTool($toolExe) {
  $cmd = Get-Command $toolExe -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }

  $kitsRoot = "C:\Program Files (x86)\Windows Kits\10\bin"
  if (-not (Test-Path $kitsRoot)) { return $null }

  $versions = Get-ChildItem -Path $kitsRoot -Directory |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
    Sort-Object -Property Name -Descending

  foreach ($ver in $versions) {
    $candidate = Join-Path $ver.FullName "x64\$toolExe"
    if (Test-Path $candidate) { return $candidate }
  }

  return $null
}

function Get-VersionFromProject($projPath) {
  if ($Version) { return $Version }
  [xml]$xml = Get-Content $projPath
  $nodes = $xml.SelectNodes("//Project/PropertyGroup/Version")
  if ($nodes -and $nodes.Count -gt 0) {
    foreach ($node in $nodes) {
      if ($node.InnerText -and $node.InnerText.Trim()) { return $node.InnerText.Trim() }
    }
  }
  return "0.0.0"
}

function Normalize-AppxVersion($value) {
  $parts = $value -split '\.'
  $numbers = New-Object System.Collections.Generic.List[int]
  foreach ($part in $parts) {
    if ($part -match '^\d+$') { $numbers.Add([int]$part) } else { $numbers.Add(0) }
  }
  while ($numbers.Count -lt 4) { $numbers.Add(0) }
  if ($numbers.Count -gt 4) { $numbers = $numbers.GetRange(0, 4) }
  return ($numbers -join '.')
}

function DetectExe($publishDir, $projectBaseName) {
  if (-not (Test-Path $publishDir)) {
    Die "No existe el directorio de publish: $publishDir"
  }

  $exe = Join-Path $publishDir "$projectBaseName.exe"
  if (Test-Path $exe) { return $exe }

  $anyExe = Get-ChildItem -Path $publishDir -Filter *.exe -File -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($anyExe) { return $anyExe.FullName }

  Die "No encuentro ningun .exe en $publishDir. Revisa el publish."
}

function CopyDirClean($src, $dst) {
  if (Test-Path $dst) { Remove-Item -Recurse -Force $dst }
  New-Item -ItemType Directory -Force -Path $dst | Out-Null
  Copy-Item -Recurse -Force -Path (Join-Path $src "*") -Destination $dst
}

function Resize-PngToFile($sourcePath, $destPath, [int]$width, [int]$height) {
  Add-Type -AssemblyName System.Drawing

  $sourceImage = [System.Drawing.Image]::FromFile($sourcePath)
  try {
    $destBitmap = New-Object System.Drawing.Bitmap $width, $height
    $destBitmap.SetResolution($sourceImage.HorizontalResolution, $sourceImage.VerticalResolution)
    $graphics = [System.Drawing.Graphics]::FromImage($destBitmap)
    try {
      $graphics.Clear([System.Drawing.Color]::Transparent)
      $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
      $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
      $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
      $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

      $scale = [Math]::Min($width / $sourceImage.Width, $height / $sourceImage.Height)
      $drawWidth = [int][Math]::Round($sourceImage.Width * $scale)
      $drawHeight = [int][Math]::Round($sourceImage.Height * $scale)
      $offsetX = [int][Math]::Floor(($width - $drawWidth) / 2)
      $offsetY = [int][Math]::Floor(($height - $drawHeight) / 2)

      $destRect = New-Object System.Drawing.Rectangle $offsetX, $offsetY, $drawWidth, $drawHeight
      $graphics.DrawImage($sourceImage, $destRect)
    } finally {
      $graphics.Dispose()
    }

    $destBitmap.Save($destPath, [System.Drawing.Imaging.ImageFormat]::Png)
  } finally {
    $sourceImage.Dispose()
    if ($destBitmap) { $destBitmap.Dispose() }
  }
}

function EnsureDirectory($path) {
  if (-not (Test-Path $path)) { New-Item -ItemType Directory -Force -Path $path | Out-Null }
}

function Create-AppxManifest($path, $exeName, $appxVersion) {
  $manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">
  <Identity Name="$IdentityName" Publisher="$Publisher" Version="$appxVersion" />
  <Properties>
    <DisplayName>$PackageName</DisplayName>
    <PublisherDisplayName>$PublisherDisplayName</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>
  <Resources>
    <Resource Language="es-ES" />
  </Resources>
  <Applications>
    <Application Id="$PackageName" Executable="$exeName" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="$PackageName"
        Description="$PackageName"
        BackgroundColor="transparent"
        Square44x44Logo="Assets\Square44x44Logo.png"
        Square150x150Logo="Assets\Square150x150Logo.png">
        <uap:DefaultTile
          Wide310x150Logo="Assets\Wide310x150Logo.png"
          Square310x310Logo="Assets\Square310x310Logo.png"
          Square71x71Logo="Assets\Square71x71Logo.png">
          <uap:ShowNameOnTiles>
            <uap:ShowOn Tile="square150x150Logo" />
            <uap:ShowOn Tile="wide310x150Logo" />
          </uap:ShowNameOnTiles>
        </uap:DefaultTile>
        <uap:SplashScreen Image="Assets\SplashScreen.png" BackgroundColor="transparent" />
      </uap:VisualElements>
    </Application>
  </Applications>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"@

  $manifest | Set-Content -Path $path -Encoding utf8
}

$root = RepoRoot
Set-Location $root

EnsureDotnet

$projPath = FindProject $root
$projBase = [System.IO.Path]::GetFileNameWithoutExtension($projPath)

$versionRaw = Get-VersionFromProject $projPath
$appxVersion = Normalize-AppxVersion $versionRaw

$artifactsRoot = Join-Path $root "artifacts\windows"
$msixRoot = Join-Path $artifactsRoot "msix"
$msixPath = Join-Path $msixRoot ("{0}_{1}_{2}.msix" -f $PackageName, $appxVersion, $Rid)

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("OfertaDemandaAvalonia\msix\" + [System.Guid]::NewGuid().ToString("N"))
$publishDir = Join-Path $tempRoot ("publish\" + $Rid)
$stagingDir = Join-Path $tempRoot "staging"
$assetsDir = Join-Path $stagingDir "Assets"

$iconSource = Join-Path $root "src\OfertaDemanda.Desktop\Assets\icon_1024.png"
if (-not (Test-Path $iconSource)) { Die "No encuentro el icono: $iconSource" }

$makeAppx = FindSdkTool "MakeAppx.exe"
if (-not $makeAppx) { Die "No encuentro MakeAppx.exe (Windows SDK requerido)." }

$signTool = FindSdkTool "SignTool.exe"
if (-not $signTool) { Die "No encuentro SignTool.exe (Windows SDK requerido)." }

EnsureDirectory $msixRoot
EnsureDirectory $publishDir

try {
  Write-Host "Repo root   : $root"
  Write-Host "Project     : $projPath"
  Write-Host "Config      : $Config"
  Write-Host "RID         : $Rid"
  Write-Host "Version     : $versionRaw (Appx: $appxVersion)"
  Write-Host "MSIX output : $msixPath"
  Write-Host "Publish dir : $publishDir"

  Write-Host "`n==> dotnet restore ..."
  dotnet restore $projPath | Out-Host

  Write-Host "`n==> dotnet build ..."
  dotnet build $projPath -c $Config -r $Rid | Out-Host

  Write-Host "`n==> dotnet publish ..."
  dotnet publish $projPath `
    -c $Config `
    -r $Rid `
    --self-contained:true `
    -p:UseAppHost=true `
    -o $publishDir | Out-Host

  $exeInPublish = DetectExe $publishDir $projBase
  $exeName = [System.IO.Path]::GetFileName($exeInPublish)
  Write-Host "Executable : $exeName"

  Write-Host "`n==> Staging files ..."
  CopyDirClean $publishDir $stagingDir
  EnsureDirectory $assetsDir

  Write-Host "==> Generating MSIX assets ..."
  $assetSpecs = @(
    @{ Name = "StoreLogo.png"; Width = 50; Height = 50 }
    @{ Name = "Square44x44Logo.png"; Width = 44; Height = 44 }
    @{ Name = "Square71x71Logo.png"; Width = 71; Height = 71 }
    @{ Name = "Square150x150Logo.png"; Width = 150; Height = 150 }
    @{ Name = "Wide310x150Logo.png"; Width = 310; Height = 150 }
    @{ Name = "Square310x310Logo.png"; Width = 310; Height = 310 }
    @{ Name = "SplashScreen.png"; Width = 620; Height = 300 }
  )

  foreach ($spec in $assetSpecs) {
    $destPath = Join-Path $assetsDir $spec.Name
    Resize-PngToFile -sourcePath $iconSource -destPath $destPath -width $spec.Width -height $spec.Height
  }

  $manifestPath = Join-Path $stagingDir "AppxManifest.xml"
  Write-Host "==> Writing AppxManifest.xml ..."
  Create-AppxManifest -path $manifestPath -exeName $exeName -appxVersion $appxVersion

  Write-Host "`n==> Packing MSIX ..."
  & $makeAppx pack /d $stagingDir /p $msixPath /o | Out-Host

  Write-Host "`n==> Signing MSIX ..."
  $pfxPath = $env:SIGN_CERT_PFX
  $pfxPassword = $env:SIGN_CERT_PASSWORD
  $createdCert = $false
  $tempPfx = $null

  if ($pfxPath) {
    if (-not (Test-Path $pfxPath)) { Die "SIGN_CERT_PFX no existe: $pfxPath" }
    if (-not $pfxPassword) { Die "SIGN_CERT_PASSWORD es obligatorio cuando SIGN_CERT_PFX esta definido." }
  } else {
    Write-Host "No se encontro SIGN_CERT_PFX. Creando certificado de desarrollo..."
    $createdCert = $true
    $cert = New-SelfSignedCertificate `
      -Subject $Publisher `
      -CertStoreLocation "Cert:\CurrentUser\My" `
      -KeyAlgorithm RSA `
      -KeyLength 2048 `
      -HashAlgorithm SHA256 `
      -KeyExportPolicy Exportable `
      -NotAfter (Get-Date).AddYears(5) `
      -Type CodeSigningCert

    $tempPfx = Join-Path $tempRoot "OfertaDemandaAvalonia.Dev.pfx"
    $cerPath = Join-Path $msixRoot "OfertaDemandaAvalonia.Dev.cer"
    $pfxPath = $tempPfx
    $pfxPassword = ([Guid]::NewGuid().ToString("N"))
    $securePassword = ConvertTo-SecureString -String $pfxPassword -Force -AsPlainText

    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword | Out-Null
    Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null
    Write-Host "Certificado exportado: $cerPath"
  }

  & $signTool sign /fd SHA256 /a /f $pfxPath /p $pfxPassword $msixPath | Out-Host

  if (-not (Test-Path $msixPath)) { Die "No se genero el MSIX: $msixPath" }

  Write-Host "`n==> Verificacion"
  Write-Host "MSIX      : $msixPath"
  Write-Host "Identity  : $IdentityName"
  Write-Host "Publisher : $Publisher"
  Write-Host "Version   : $appxVersion"

  if ($createdCert) {
    Write-Host "Cert CER  : $cerPath"
  }

  Write-Host "`nMSIX listo."
} finally {
  if (Test-Path $tempRoot) { Remove-Item -Recurse -Force $tempRoot }
}
