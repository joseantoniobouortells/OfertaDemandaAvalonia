<#
windows_install_app.ps1
Publishes a .NET/Avalonia desktop app and "installs" it for current user on Windows 11:
- dotnet publish
- copies output to %LOCALAPPDATA%\Programs\<AppName>
- creates Start Menu shortcut (%APPDATA%\Microsoft\Windows\Start Menu\Programs)

Usage (PowerShell):
  powershell -ExecutionPolicy Bypass -File .\scripts\windows_install_app.ps1
  powershell -ExecutionPolicy Bypass -File .\scripts\windows_install_app.ps1 -Install
  powershell -ExecutionPolicy Bypass -File .\scripts\windows_install_app.ps1 -Install -AppName "OfertaDemandaAvalonia"
  powershell -ExecutionPolicy Bypass -File .\scripts\windows_install_app.ps1 -Uninstall -AppName "OfertaDemandaAvalonia"
#>

[CmdletBinding()]
param(
  [string]$Project = "",
  [string]$AppName = "",

  [ValidateSet("win-x64","win-arm64")]
  [string]$Rid = "win-x64",

  [ValidateSet("Release","Debug")]
  [string]$Config = "Release",

  [bool]$SelfContained = $true,

  [string]$Version = "1.0.0",
  [string]$Company = "OfertaDemanda",

  [switch]$Install,
  [switch]$Uninstall,
  [switch]$Clean
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
    Die "dotnet no está instalado o no está en PATH."
  }
}

function DetectExe($publishDir, $projectBaseName) {
  if (-not (Test-Path $publishDir)) {
    Die "No existe el directorio de publish: $publishDir"
  }

  $exe = Join-Path $publishDir "$projectBaseName.exe"
  if (Test-Path $exe) { return $exe }

  $anyExe = Get-ChildItem -Path $publishDir -Filter *.exe -File -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($anyExe) { return $anyExe.FullName }

  Die "No encuentro ningún .exe en $publishDir. Revisa el publish."
}

function CopyDirClean($src, $dst) {
  if (Test-Path $dst) { Remove-Item -Recurse -Force $dst }
  New-Item -ItemType Directory -Force -Path $dst | Out-Null
  Copy-Item -Recurse -Force -Path (Join-Path $src "*") -Destination $dst
}

function StartMenuShortcutPath($appName) {
  $programs = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
  return Join-Path $programs "$appName.lnk"
}

function CreateShortcut($lnkPath, $targetExe, $workingDir, $appName) {
  $shell = New-Object -ComObject WScript.Shell
  $shortcut = $shell.CreateShortcut($lnkPath)
  $shortcut.TargetPath = $targetExe
  $shortcut.WorkingDirectory = $workingDir
  $shortcut.WindowStyle = 1
  $shortcut.Description = $appName
  $shortcut.Save()
}

$root = RepoRoot
Set-Location $root

EnsureDotnet

$projPath = FindProject $root
$projBase = [System.IO.Path]::GetFileNameWithoutExtension($projPath)

if (-not $AppName) { $AppName = $projBase }

$artifactsRoot = Join-Path $root "artifacts\windows"
$publishDir = Join-Path $artifactsRoot ("publish\" + $Rid)
$installDir = Join-Path $env:LOCALAPPDATA ("Programs\" + $AppName)
$shortcutPath = StartMenuShortcutPath $AppName

Write-Host "Repo root    : $root"
Write-Host "Project      : $projPath"
Write-Host "Config       : $Config"
Write-Host "RID          : $Rid"
Write-Host "SelfContained: $SelfContained"
Write-Host "Publish dir  : $publishDir"
Write-Host "Install dir  : $installDir"
Write-Host "Shortcut     : $shortcutPath"

if ($Uninstall) {
  Write-Host "`n==> Uninstall (per-user) ..."
  if (Test-Path $shortcutPath) { Remove-Item -Force $shortcutPath }
  if (Test-Path $installDir) { Remove-Item -Recurse -Force $installDir }
  Write-Host "Desinstalado. (Borrado acceso directo y carpeta de instalación)"
  exit 0
}

if ($Clean) {
  Write-Host "`n==> Clean artifacts ..."
  if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
}

Write-Host "`n==> dotnet publish ..."
dotnet publish $projPath `
  -c $Config `
  -r $Rid `
  --self-contained:$SelfContained `
  -p:UseAppHost=true `
  -o $publishDir | Out-Host

$exeInPublish = DetectExe $publishDir $projBase
Write-Host "`nExecutable (publish): $exeInPublish"

if ($Install) {
  Write-Host "`n==> Installing (copy files) ..."
  CopyDirClean $publishDir $installDir

  $exeInstalled = Join-Path $installDir ([System.IO.Path]::GetFileName($exeInPublish))
  if (-not (Test-Path $exeInstalled)) {
    Die "No encuentro el exe instalado: $exeInstalled"
  }

  Write-Host "==> Creating Start Menu shortcut ..."
  CreateShortcut -lnkPath $shortcutPath -targetExe $exeInstalled -workingDir $installDir -appName $AppName

  Write-Host "`nInstalación completada."
  Write-Host "Lanzando aplicación..."
  Start-Process $exeInstalled
} else {
  Write-Host "`nPublish completado. Para instalar, ejecuta con -Install."
  Write-Host "Para ejecutar desde publish:"
  Write-Host "  `"$exeInPublish`""
}
