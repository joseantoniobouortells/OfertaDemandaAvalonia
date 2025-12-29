<#
bootstrap_windows.ps1
Prepara entorno Windows para repo .NET/Avalonia:
- Instala/verifica Git, .NET SDK 8, (opcional) VS Code, PowerShell 7
- Verifica dotnet restore/build/test
#>

[CmdletBinding()]
param(
  [switch]$InstallVSCode,
  [switch]$InstallPwsh7
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Have($cmd) { return [bool](Get-Command $cmd -ErrorAction SilentlyContinue) }

function WingetInstall($id) {
  Write-Host "==> Installing $id ..."
  winget install --id $id --exact --accept-source-agreements --accept-package-agreements
}

if (-not (Have "winget")) { throw "winget no está disponible. Actualiza App Installer desde Microsoft Store." }

# Git
if (-not (Have "git")) { WingetInstall "Git.Git" } else { Write-Host "Git OK" }

# .NET 8 SDK
if (-not (Have "dotnet")) {
  WingetInstall "Microsoft.DotNet.SDK.8"
} else {
  $ver = & dotnet --version
  Write-Host "dotnet OK ($ver)"
}

if ($InstallVSCode) {
  if (-not (Have "code")) { WingetInstall "Microsoft.VisualStudioCode" } else { Write-Host "VS Code OK" }
}

if ($InstallPwsh7) {
  if (-not (Have "pwsh")) { WingetInstall "Microsoft.PowerShell" } else { Write-Host "PowerShell 7 OK" }
}

Write-Host "`n==> Verifying build ..."
& dotnet restore
& dotnet build -c Release
& dotnet test -c Release

Write-Host "`nDone."
