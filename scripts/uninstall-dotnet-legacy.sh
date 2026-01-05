#!/usr/bin/env bash
set -euo pipefail

if [[ "${EUID}" -ne 0 ]]; then
  echo "Este script requiere sudo (root)." >&2
  echo "Ejecuta: sudo scripts/uninstall-dotnet-legacy.sh" >&2
  exit 1
fi

paths=(
  /usr/local/share/dotnet/sdk/6.*
  /usr/local/share/dotnet/sdk/7.*
  /usr/local/share/dotnet/shared/Microsoft.NETCore.App/6.*
  /usr/local/share/dotnet/shared/Microsoft.NETCore.App/7.*
  /usr/local/share/dotnet/shared/Microsoft.AspNetCore.App/6.*
  /usr/local/share/dotnet/shared/Microsoft.AspNetCore.App/7.*
)

echo "Eliminando SDKs y runtimes .NET 6.x/7.x..."
rm -rf "${paths[@]}"
echo "Listo. SDKs actuales:"
dotnet --list-sdks || true
