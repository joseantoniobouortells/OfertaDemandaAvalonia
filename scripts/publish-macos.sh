#!/usr/bin/env bash
set -euo pipefail

# publish-macos.sh
# Compila, hace publish y genera un .dmg en artifacts/macos/dmg
# Sin parámetros. Diseñado para ejecutarse desde cualquier ruta.

die() { echo "ERROR: $*" >&2; exit 1; }

command -v dotnet >/dev/null 2>&1 || die "dotnet no está disponible en PATH"
command -v hdiutil >/dev/null 2>&1 || die "hdiutil no está disponible (debería venir en macOS)"
command -v plutil >/dev/null 2>&1 || die "plutil no está disponible (debería venir en macOS)"

# Repo root = padre de la carpeta donde vive este script (scripts/)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$ROOT_DIR"

# Detecta RID por arquitectura
ARCH="$(uname -m)"
if [[ "$ARCH" == "arm64" ]]; then
  RID="osx-arm64"
else
  RID="osx-x64"
fi

CONFIG="Release"
SELF_CONTAINED="true"

ARTIFACTS_DIR="$ROOT_DIR/artifacts/macos"
DMG_DIR="$ARTIFACTS_DIR/dmg"

TMP_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/ofertademanda-macos.XXXXXX")"
trap 'rm -rf "$TMP_ROOT"' EXIT

PUBLISH_DIR="$TMP_ROOT/publish/$RID"
APP_OUT_DIR="$TMP_ROOT/app"
DMG_STAGING_ROOT="$TMP_ROOT/dmg-staging"

mkdir -p "$ARTIFACTS_DIR" "$DMG_DIR" "$PUBLISH_DIR" "$APP_OUT_DIR" "$DMG_STAGING_ROOT"

# Intento 1: ruta “típica” si la tienes
DEFAULT_CSPROJ="$ROOT_DIR/src/OfertaDemanda.Desktop/OfertaDemanda.Desktop.csproj"

# Encuentra un .csproj “desktop/avalonia” si el default no existe
if [[ -f "$DEFAULT_CSPROJ" ]]; then
  CSPROJ="$DEFAULT_CSPROJ"
else
  CSPROJ="$(find "$ROOT_DIR/src" -name "*.csproj" 2>/dev/null | grep -E -i '(desktop|avalonia)' | head -n 1 || true)"
  [[ -n "$CSPROJ" ]] || die "No se ha encontrado el .csproj. Ajusta DEFAULT_CSPROJ o revisa la estructura /src."
fi

APP_NAME="$(basename "$CSPROJ" .csproj)"

# Extrae Version si existe en el csproj (fallback: 0.0.0)
VERSION="$(grep -m1 -E '<Version>[^<]+' "$CSPROJ" | sed -E 's/.*<Version>([^<]+)<\/Version>.*/\1/' || true)"
VERSION="${VERSION:-0.0.0}"

# Identificador bundle (simple y estable)
APP_ID_BASE="$(echo "$APP_NAME" | tr '[:upper:]' '[:lower:]' | tr -cd 'a-z0-9')"
BUNDLE_ID="com.${APP_ID_BASE}.${APP_ID_BASE}"

echo "==> Repo root     : $ROOT_DIR"
echo "==> Project       : $CSPROJ"
echo "==> AppName       : $APP_NAME"
echo "==> Version       : $VERSION"
echo "==> RID           : $RID"
echo "==> Config        : $CONFIG"
echo "==> SelfContained : $SELF_CONTAINED"
echo "==> Publish dir   : $PUBLISH_DIR"

echo
echo "==> dotnet restore"
dotnet restore "$CSPROJ"

echo
echo "==> dotnet publish"
rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR"

dotnet publish "$CSPROJ" \
  -c "$CONFIG" \
  -r "$RID" \
  --self-contained:"$SELF_CONTAINED" \
  -p:UseAppHost=true \
  -o "$PUBLISH_DIR"

# El ejecutable apphost suele llamarse igual que el proyecto
EXEC_PATH="$PUBLISH_DIR/$APP_NAME"
if [[ ! -f "$EXEC_PATH" ]]; then
  # Fallback: busca un binario ejecutable sin extensión en la raíz del publish
  EXEC_PATH="$(find "$PUBLISH_DIR" -maxdepth 1 -type f -perm +111 2>/dev/null | head -n 1 || true)"
fi
[[ -n "${EXEC_PATH:-}" && -f "$EXEC_PATH" ]] || die "No se ha encontrado el ejecutable en el publish. Revisa $PUBLISH_DIR"

EXEC_NAME="$(basename "$EXEC_PATH")"
chmod +x "$EXEC_PATH" || true

# Construye .app
APP_DIR="$APP_OUT_DIR/${APP_NAME}.app"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RES_DIR="$CONTENTS_DIR/Resources"

echo
echo "==> Building .app bundle: $APP_DIR"

rm -rf "$APP_DIR"
mkdir -p "$MACOS_DIR" "$RES_DIR"

ICON_SRC="$ROOT_DIR/src/OfertaDemanda.Desktop/Assets/icon.icns"
if [[ -f "$ICON_SRC" ]]; then
  cp "$ICON_SRC" "$RES_DIR/AppIcon.icns"
fi

# Copia TODO el publish dentro de Contents/MacOS (simple y robusto)
cp -R "$PUBLISH_DIR/"* "$MACOS_DIR/"

# Info.plist mínimo
INFO_PLIST="$CONTENTS_DIR/Info.plist"
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
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleExecutable</key>
  <string>${EXEC_NAME}</string>
  <key>LSMinimumSystemVersion</key>
  <string>11.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
  <key>CFBundleIconFile</key>
  <string>AppIcon.icns</string>
</dict>
</plist>
EOF

plutil -lint "$INFO_PLIST" >/dev/null || die "Info.plist inválido"

# Genera DMG (drag&drop a Applications)
DMG_NAME="${APP_NAME}-${VERSION}-${RID}.dmg"
DMG_PATH="$DMG_DIR/$DMG_NAME"
STAGING_DIR="$DMG_STAGING_ROOT/$APP_NAME"

echo
echo "==> Packaging DMG: $DMG_PATH"

rm -rf "$STAGING_DIR"
mkdir -p "$STAGING_DIR"

cp -R "$APP_DIR" "$STAGING_DIR/"
ln -s /Applications "$STAGING_DIR/Applications"

rm -f "$DMG_PATH" 2>/dev/null || true
create_dmg() {
  hdiutil create \
    -volname "$APP_NAME" \
    -srcfolder "$STAGING_DIR" \
    -ov \
    -format UDZO \
    "$DMG_PATH" >/dev/null
}

for attempt in 1 2 3; do
  rm -f "$DMG_PATH" 2>/dev/null || true
  if create_dmg; then
    break
  fi
  if [[ "$attempt" -eq 3 ]]; then
    die "No se pudo crear el DMG (hdiutil: resource busy)."
  fi
  sleep 2
done

echo
echo "==> OK"
echo "APP : $APP_DIR"
echo "DMG : $DMG_PATH"
