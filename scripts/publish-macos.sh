#!/usr/bin/env bash
set -euo pipefail

# macos_bundle_app.sh
# Publica un proyecto Avalonia (.NET) y genera un .app bundle en macOS.
# Opcionalmente lo instala en /Applications.
#
# Requisitos:
# - macOS
# - dotnet SDK
# - comando `file` (incluido en macOS)
#
# Uso:
#   ./scripts/macos_bundle_app.sh
#   ./scripts/macos_bundle_app.sh --project src/OfertaDemanda.Desktop/OfertaDemanda.Desktop.csproj --app-name OfertaDemandaAvalonia --install
#
# Flags:
#   --project <path>          Ruta al .csproj (default: autodetección)
#   --app-name <name>         Nombre del .app (default: nombre del proyecto)
#   --rid <osx-arm64|osx-x64> RID (default: detectado por arquitectura)
#   --config <Release|Debug>  Configuración (default: Release)
#   --self-contained <true|false> Publicación self-contained (default: true)
#   --bundle-id <id>          CFBundleIdentifier (default: com.example.<appname>)
#   --version <x.y.z>         Versión (default: 1.0.0)
#   --icon <path.icns>        Icono .icns a incluir (opcional)
#   --install                Copia el .app a /Applications (puede requerir sudo)
#   --clean                  Borra artifacts previos del mismo nombre antes de generar
#   --help                   Ayuda

print_help() {
  sed -n '1,120p' "$0" | sed 's/^# \{0,1\}//'
}

die() {
  echo "ERROR: $*" >&2
  exit 1
}

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

PROJECT_PATH=""
APP_NAME=""
RID=""
CONFIG="Release"
SELF_CONTAINED="true"
BUNDLE_ID=""
VERSION="1.0.0"
ICON_PATH=""
DO_INSTALL="false"
DO_CLEAN="false"

# Parse args
while [[ $# -gt 0 ]]; do
  case "$1" in
    --project) PROJECT_PATH="${2:-}"; shift 2;;
    --app-name) APP_NAME="${2:-}"; shift 2;;
    --rid) RID="${2:-}"; shift 2;;
    --config) CONFIG="${2:-}"; shift 2;;
    --self-contained) SELF_CONTAINED="${2:-}"; shift 2;;
    --bundle-id) BUNDLE_ID="${2:-}"; shift 2;;
    --version) VERSION="${2:-}"; shift 2;;
    --icon) ICON_PATH="${2:-}"; shift 2;;
    --install) DO_INSTALL="true"; shift;;
    --clean) DO_CLEAN="true"; shift;;
    --help|-h) print_help; exit 0;;
    *) die "Argumento desconocido: $1";;
  esac
done

cd "$REPO_ROOT"

command -v dotnet >/dev/null 2>&1 || die "dotnet no está instalado o no está en PATH"
command -v file >/dev/null 2>&1 || die "El comando 'file' no está disponible (debería venir en macOS)"

# Detect RID
if [[ -z "$RID" ]]; then
  ARCH="$(uname -m)"
  if [[ "$ARCH" == "arm64" ]]; then
    RID="osx-arm64"
  else
    RID="osx-x64"
  fi
fi

# Autodetect project
if [[ -z "$PROJECT_PATH" ]]; then
  if [[ -f "src/OfertaDemanda.Desktop/OfertaDemanda.Desktop.csproj" ]]; then
    PROJECT_PATH="src/OfertaDemanda.Desktop/OfertaDemanda.Desktop.csproj"
  else
    # Busca un .csproj dentro de src que parezca "Desktop" o "Avalonia"
    FOUND="$(find src -maxdepth 3 -name "*.csproj" 2>/dev/null | grep -Ei '(desktop|avalonia)' | head -n 1 || true)"
    [[ -n "$FOUND" ]] || die "No pude autodetectar el .csproj. Usa --project <ruta>"
    PROJECT_PATH="$FOUND"
  fi
fi

[[ -f "$PROJECT_PATH" ]] || die "No existe el proyecto: $PROJECT_PATH"

PROJECT_DIR="$(cd "$(dirname "$PROJECT_PATH")" && pwd)"
PROJECT_FILE="$(basename "$PROJECT_PATH")"
PROJECT_BASENAME="${PROJECT_FILE%.csproj}"

# App name default
if [[ -z "$APP_NAME" ]]; then
  APP_NAME="$PROJECT_BASENAME"
fi

# Bundle id default
if [[ -z "$BUNDLE_ID" ]]; then
  # Normaliza a minúsculas y quita espacios
  SAFE_APP="$(echo "$APP_NAME" | tr '[:upper:]' '[:lower:]' | tr -cd 'a-z0-9.-')"
  [[ -n "$SAFE_APP" ]] || SAFE_APP="app"
  BUNDLE_ID="com.example.${SAFE_APP}"
fi

ARTIFACTS_DIR="$REPO_ROOT/artifacts"
PUBLISH_DIR="$ARTIFACTS_DIR/publish/$RID"
APP_DIR="$ARTIFACTS_DIR/${APP_NAME}.app"

if [[ "$DO_CLEAN" == "true" ]]; then
  rm -rf "$PUBLISH_DIR" "$APP_DIR"
fi

mkdir -p "$ARTIFACTS_DIR"

echo "Repo root      : $REPO_ROOT"
echo "Proyecto       : $PROJECT_PATH"
echo "Config         : $CONFIG"
echo "RID            : $RID"
echo "Self-contained : $SELF_CONTAINED"
echo "Publish dir    : $PUBLISH_DIR"
echo "App bundle     : $APP_DIR"

# Publish
echo
echo "==> dotnet publish ..."
dotnet publish "$PROJECT_PATH" \
  -c "$CONFIG" \
  -r "$RID" \
  --self-contained "$SELF_CONTAINED" \
  -p:UseAppHost=true \
  -o "$PUBLISH_DIR"

# Detect executable inside publish output
# Preferimos un Mach-O sin extensión.
echo
echo "==> Detectando ejecutable ..."
EXECUTABLE_NAME=""

# 1) Buscar Mach-O (sin extensión) en el publish dir
while IFS= read -r candidate; do
  base="$(basename "$candidate")"
  # descartar .dylib, .dll, etc.
  if [[ "$base" != *.* ]]; then
    if file "$candidate" | grep -q "Mach-O"; then
      EXECUTABLE_NAME="$base"
      break
    fi
  fi
done < <(find "$PUBLISH_DIR" -maxdepth 1 -type f -perm -111 2>/dev/null | sort)

# 2) Fallback: si no hay Mach-O, usar el nombre base del dll principal si existe
if [[ -z "$EXECUTABLE_NAME" ]]; then
  MAIN_DLL="$PUBLISH_DIR/${PROJECT_BASENAME}.dll"
  if [[ -f "$MAIN_DLL" ]]; then
    EXECUTABLE_NAME="$PROJECT_BASENAME"
  else
    # último fallback: primer .dll (no ideal)
    ANY_DLL="$(find "$PUBLISH_DIR" -maxdepth 1 -name "*.dll" | head -n 1 || true)"
    [[ -n "$ANY_DLL" ]] || die "No encuentro ejecutable ni dll en $PUBLISH_DIR"
    EXECUTABLE_NAME="$(basename "$ANY_DLL" .dll)"
  fi
fi

echo "Ejecutable      : $EXECUTABLE_NAME"

# Create .app structure and copy
echo
echo "==> Creando bundle .app ..."
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

cp -R "$PUBLISH_DIR"/. "$APP_DIR/Contents/MacOS/"

# Info.plist
INFO_PLIST="$APP_DIR/Contents/Info.plist"

ICON_KEY_BLOCK=""
ICON_COPY_BLOCK=""

if [[ -n "$ICON_PATH" ]]; then
  [[ -f "$ICON_PATH" ]] || die "No existe el icono: $ICON_PATH"
  ICON_FILENAME="$(basename "$ICON_PATH")"
  cp "$ICON_PATH" "$APP_DIR/Contents/Resources/$ICON_FILENAME"
  # CFBundleIconFile puede ir sin extensión o con; lo dejamos con nombre completo.
  ICON_KEY_BLOCK=$'\n  <key>CFBundleIconFile</key>\n  <string>'"${ICON_FILENAME}"$'</string>\n'
fi

cat > "$INFO_PLIST" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>
  <string>${APP_NAME}</string>
  <key>CFBundleDisplayName</key>
  <string>${APP_NAME}</string>
  <key>CFBundleIdentifier</key>
  <string>${BUNDLE_ID}</string>
  <key>CFBundleVersion</key>
  <string>${VERSION}</string>
  <key>CFBundleShortVersionString</key>
  <string>${VERSION}</string>
  <key>CFBundleExecutable</key>
  <string>${EXECUTABLE_NAME}</string>
  <key>NSHighResolutionCapable</key>
  <true/>${ICON_KEY_BLOCK}
</dict>
</plist>
EOF

# Ensure executable bit
chmod +x "$APP_DIR/Contents/MacOS/$EXECUTABLE_NAME" || true

# Remove quarantine attributes (harmless if not present)
xattr -dr com.apple.quarantine "$APP_DIR" 2>/dev/null || true

echo
echo "==> Bundle generado:"
echo "   $APP_DIR"

# Quick launch test
echo
echo "==> Probando apertura (open) ..."
open "$APP_DIR" || true

# Install
if [[ "$DO_INSTALL" == "true" ]]; then
  echo
  echo "==> Instalando en /Applications ..."
  DEST="/Applications/${APP_NAME}.app"
  rm -rf "$DEST" 2>/dev/null || true

  if cp -R "$APP_DIR" /Applications/ 2>/dev/null; then
    echo "Instalado en: $DEST"
    open "$DEST" || true
  else
    echo "No he podido copiar a /Applications (permiso). Ejecuta:"
    echo "  sudo cp -R \"$APP_DIR\" /Applications/"
    echo "Luego:"
    echo "  open \"/Applications/${APP_NAME}.app\""
    exit 0
  fi
fi

echo
echo "Hecho."
echo "Siguiente: si quieres instalar, vuelve a ejecutar con --install"
