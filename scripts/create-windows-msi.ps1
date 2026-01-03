<#
create-windows-msi.ps1
Builds and packages the Avalonia desktop app into a Windows MSI using WiX Toolset v4.

Usage (PowerShell):
  powershell -ExecutionPolicy Bypass -File .\scripts\create-windows-msi.ps1
#>

[CmdletBinding()]
param(
  [string]$Project = "",

  [ValidateSet("win-x64","win-arm64")]
  [string]$Rid = "win-x64",

  [ValidateSet("Release","Debug")]
  [string]$Config = "Release",

  [string]$PackageName = "OfertaDemandaAvalonia",
  [string]$Manufacturer = "OfertaDemanda",
  [string]$UpgradeCode = "B703C2E3-548B-4C2A-9D87-9A6A0E50D3D2",
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

function EnsureDirectory($path) {
  if (-not (Test-Path $path)) { New-Item -ItemType Directory -Force -Path $path | Out-Null }
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

function Normalize-MsiVersion($value) {
  $parts = $value -split '\.'
  $numbers = New-Object System.Collections.Generic.List[int]
  foreach ($part in $parts) {
    if ($part -match '^\d+$') { $numbers.Add([int]$part) } else { $numbers.Add(0) }
  }
  while ($numbers.Count -lt 3) { $numbers.Add(0) }
  if ($numbers.Count -gt 3) { $numbers = $numbers.GetRange(0, 3) }
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

function FindWixCli {
  $cmd = Get-Command wix -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  return $null
}

function EnsureWixCli($toolsDir) {
  $wixExe = FindWixCli
  if ($wixExe) { return $wixExe }

  EnsureDirectory $toolsDir
  $localExe = Join-Path $toolsDir "wix.exe"
  if (Test-Path $localExe) { return $localExe }

  Write-Host "WiX Toolset v4 no encontrado. Instalando wix..."
  dotnet tool install wix --tool-path $toolsDir | Out-Host
  if (-not (Test-Path $localExe)) {
    Die "No se pudo instalar wix. Instala WiX v4 (dotnet tool install wix) y reintenta."
  }

  return $localExe
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

function Convert-PngToIco($pngPath, $icoPath) {
  $pngBytes = [System.IO.File]::ReadAllBytes($pngPath)
  $stream = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
  $writer = New-Object System.IO.BinaryWriter($stream)
  try {
    $writer.Write([UInt16]0) # reserved
    $writer.Write([UInt16]1) # type
    $writer.Write([UInt16]1) # count
    $writer.Write([Byte]0)   # width 256
    $writer.Write([Byte]0)   # height 256
    $writer.Write([Byte]0)   # colors
    $writer.Write([Byte]0)   # reserved
    $writer.Write([UInt16]1) # planes
    $writer.Write([UInt16]32) # bpp
    $writer.Write([UInt32]$pngBytes.Length)
    $writer.Write([UInt32]22) # offset
    $writer.Write($pngBytes)
  } finally {
    $writer.Dispose()
    $stream.Dispose()
  }
}

function New-DeterministicGuid($value) {
  $md5 = [System.Security.Cryptography.MD5]::Create()
  try {
    $bytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($value.ToLowerInvariant()))
    return [Guid]::new($bytes)
  } finally {
    $md5.Dispose()
  }
}

function New-DeterministicId($prefix, $value) {
  $sha1 = [System.Security.Cryptography.SHA1]::Create()
  try {
    $bytes = $sha1.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($value.ToLowerInvariant()))
    $hex = ($bytes | ForEach-Object { $_.ToString("x2") }) -join ""
    return ($prefix + $hex.Substring(0, 16))
  } finally {
    $sha1.Dispose()
  }
}

function Escape-Xml($value) {
  if ($null -eq $value) { return "" }
  return [System.Security.SecurityElement]::Escape($value)
}

function Write-HarvestDirectory($dirPath, $relativeDir, $sb, $componentIds) {
  $subdirs = Get-ChildItem -Path $dirPath -Directory | Sort-Object Name
  $files = Get-ChildItem -Path $dirPath -File | Sort-Object Name

  foreach ($file in $files) {
    $relativePath = if ($relativeDir) { Join-Path $relativeDir $file.Name } else { $file.Name }
    $componentId = New-DeterministicId "cmp" $relativePath
    $fileId = New-DeterministicId "fil" $relativePath
    $componentGuid = New-DeterministicGuid $relativePath
    $componentIds.Add($componentId) | Out-Null

    $null = $sb.AppendLine("    <Component Id=""$componentId"" Guid=""$componentGuid"">")
    $null = $sb.AppendLine("      <File Id=""$fileId"" Source=""`$(var.SourceDir)\$relativePath"" KeyPath=""yes"" />")
    $null = $sb.AppendLine("    </Component>")
  }

  foreach ($subdir in $subdirs) {
    $childRelative = if ($relativeDir) { Join-Path $relativeDir $subdir.Name } else { $subdir.Name }
    $dirId = New-DeterministicId "dir" $childRelative
    $dirName = Escape-Xml $subdir.Name
    $null = $sb.AppendLine("    <Directory Id=""$dirId"" Name=""$dirName"">")
    Write-HarvestDirectory -dirPath $subdir.FullName -relativeDir $childRelative -sb $sb -componentIds $componentIds
    $null = $sb.AppendLine("    </Directory>")
  }
}

function Write-HarvestWxs($publishDir, $harvestWxs) {
  $componentIds = New-Object System.Collections.Generic.List[string]
  $sb = New-Object System.Text.StringBuilder
  $null = $sb.AppendLine("<?xml version=""1.0"" encoding=""utf-8""?>")
  $null = $sb.AppendLine("<Wix xmlns=""http://wixtoolset.org/schemas/v4/wxs"">")
  $null = $sb.AppendLine("  <Fragment>")
  $null = $sb.AppendLine("    <DirectoryRef Id=""INSTALLFOLDER"">")
  Write-HarvestDirectory -dirPath $publishDir -relativeDir "" -sb $sb -componentIds $componentIds
  $null = $sb.AppendLine("    </DirectoryRef>")
  $null = $sb.AppendLine("  </Fragment>")
  $null = $sb.AppendLine("  <Fragment>")
  $null = $sb.AppendLine("    <ComponentGroup Id=""AppFiles"">")
  foreach ($id in $componentIds) {
    $null = $sb.AppendLine("      <ComponentRef Id=""$id"" />")
  }
  $null = $sb.AppendLine("    </ComponentGroup>")
  $null = $sb.AppendLine("  </Fragment>")
  $null = $sb.AppendLine("</Wix>")

  $sb.ToString() | Set-Content -Path $harvestWxs -Encoding utf8
}

$root = RepoRoot
Set-Location $root

EnsureDotnet

$projPath = FindProject $root
$projBase = [System.IO.Path]::GetFileNameWithoutExtension($projPath)

$versionRaw = Get-VersionFromProject $projPath
$msiVersion = Normalize-MsiVersion $versionRaw

$artifacts = Join-Path $root "artifacts"
$publishDir = Join-Path $artifacts ("publish\" + $Rid)
$msiRoot = Join-Path $artifacts "msi"
$msiPath = Join-Path $msiRoot ("{0}_{1}_{2}.msi" -f $PackageName, $msiVersion, $Rid)

$wixDir = Join-Path $root "installer\wix"
$packageWxs = Join-Path $wixDir "Package.wxs"
$harvestWxs = Join-Path $wixDir "HarvestedFiles.wxs"

$toolsDir = Join-Path $artifacts "tools\wix"
$wixExe = EnsureWixCli $toolsDir

$iconSource = Join-Path $root "src\OfertaDemanda.Desktop\Assets\icon_1024.png"
if (-not (Test-Path $iconSource)) { Die "No encuentro el icono: $iconSource" }

EnsureDirectory $artifacts
EnsureDirectory $msiRoot
EnsureDirectory $wixDir

Write-Host "Repo root  : $root"
Write-Host "Project    : $projPath"
Write-Host "Config     : $Config"
Write-Host "RID        : $Rid"
Write-Host "Version    : $versionRaw (MSI: $msiVersion)"
Write-Host "Wix CLI    : $wixExe"
Write-Host "MSI output : $msiPath"

if (-not (Test-Path $packageWxs)) {
  Die "No existe $packageWxs. Revisa installer\\wix\\Package.wxs."
}

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

Write-Host "`n==> Generating icon (.ico) ..."
$iconPng = Join-Path $msiRoot "icon_256.png"
$iconIco = Join-Path $msiRoot "app.ico"
Resize-PngToFile -sourcePath $iconSource -destPath $iconPng -width 256 -height 256
Convert-PngToIco -pngPath $iconPng -icoPath $iconIco

Write-Host "`n==> Harvesting publish directory ..."
Write-HarvestWxs -publishDir $publishDir -harvestWxs $harvestWxs

Write-Host "`n==> Building MSI ..."
$arch = if ($Rid -eq "win-arm64") { "arm64" } else { "x64" }
& $wixExe build $packageWxs $harvestWxs `
  -o $msiPath `
  -arch $arch `
  -d ProductName=$PackageName `
  -d Manufacturer=$Manufacturer `
  -d ProductVersion=$msiVersion `
  -d UpgradeCode=$UpgradeCode `
  -d SourceDir=$publishDir `
  -d IconPath=$iconIco `
  -d ExeName=$exeName | Out-Host

if (-not (Test-Path $msiPath)) { Die "No se genero el MSI: $msiPath" }

Write-Host "`n==> Verificacion"
Write-Host "MSI       : $msiPath"
Write-Host "Harvest   : $harvestWxs"
Write-Host "Product   : $PackageName"
Write-Host "Version   : $msiVersion"
Write-Host "Upgrade   : $UpgradeCode"

Write-Host "`nMSI listo."
