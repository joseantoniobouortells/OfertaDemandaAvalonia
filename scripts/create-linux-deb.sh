#!/usr/bin/env bash
set -euo pipefail

# create-linux-deb.sh
# Genera un paquete .deb a partir del publish de Linux (x64 y opcionalmente arm64).
# Sin parámetros. Diseñado para ejecutarse desde cualquier ruta.

die() { echo "ERROR: $*" >&2; exit 1; }

command -v dotnet >/dev/null 2>&1 || die "dotnet no está disponible en PATH"
command -v dpkg-deb >/dev/null 2>&1 || die "dpkg-deb no está disponible (instala dpkg)"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$ROOT_DIR"

CONFIG="Release"
SELF_CONTAINED="true"

ARTIFACTS_DIR="$ROOT_DIR/artifacts/linux"
DEB_OUTPUT_DIR="$ARTIFACTS_DIR/deb"

TMP_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/ofertademanda-linux-deb.XXXXXX")"
trap 'rm -rf "$TMP_ROOT"' EXIT

PUBLISH_ROOT="$TMP_ROOT/publish"
STAGING_ROOT="$TMP_ROOT/deb-staging"

mkdir -p "$ARTIFACTS_DIR" "$DEB_OUTPUT_DIR" "$PUBLISH_ROOT" "$STAGING_ROOT"

DEFAULT_CSPROJ="$ROOT_DIR/src/OfertaDemanda.Desktop/OfertaDemanda.Desktop.csproj"
if [[ -f "$DEFAULT_CSPROJ" ]]; then
  CSPROJ="$DEFAULT_CSPROJ"
else
  CSPROJ="$(find "$ROOT_DIR/src" -name "*.csproj" 2>/dev/null | grep -E -i '(desktop|avalonia)' | head -n 1 || true)"
  [[ -n "$CSPROJ" ]] || die "No se ha encontrado el .csproj. Ajusta DEFAULT_CSPROJ o revisa la estructura /src."
fi

APP_NAME="$(basename "$CSPROJ" .csproj)"
PACKAGE_NAME="$(echo "$APP_NAME" | tr '[:upper:]' '[:lower:]')"
APP_DISPLAY_NAME="OfertaDemanda"
APP_DESCRIPTION="Simulador visual de microeconomía"

VERSION="$(grep -m1 -E '<Version>[^<]+' "$CSPROJ" | sed -E 's/.*<Version>([^<]+)<\/Version>.*/\1/' || true)"
VERSION="${VERSION:-0.0.0}"

ICON_SRC="$ROOT_DIR/src/OfertaDemanda.Desktop/Assets/icon_1024.png"
[[ -f "$ICON_SRC" ]] || die "No encuentro el icono: $ICON_SRC"

echo "==> Repo root     : $ROOT_DIR"
echo "==> Project       : $CSPROJ"
echo "==> AppName       : $APP_NAME"
echo "==> Version       : $VERSION"
echo "==> Config        : $CONFIG"
echo "==> SelfContained : $SELF_CONTAINED"

RIDS=("linux-x64")
if [[ -d "$PUBLISH_ROOT/linux-arm64" ]]; then
  RIDS+=("linux-arm64")
fi

for RID in "${RIDS[@]}"; do
  echo
  echo "==> Preparando RID: $RID"

  PUBLISH_DIR="$PUBLISH_ROOT/$RID"
  if [[ ! -d "$PUBLISH_DIR" || -z "$(ls -A "$PUBLISH_DIR" 2>/dev/null)" ]]; then
    echo "==> dotnet publish ($RID)"
    rm -rf "$PUBLISH_DIR"
    mkdir -p "$PUBLISH_DIR"
    dotnet publish "$CSPROJ" \
      -c "$CONFIG" \
      -r "$RID" \
      --self-contained:"$SELF_CONTAINED" \
      -p:UseAppHost=true \
      -o "$PUBLISH_DIR"
  else
    echo "==> Publish existente detectado: $PUBLISH_DIR"
  fi

  EXEC_PATH="$PUBLISH_DIR/$APP_NAME"
  if [[ ! -f "$EXEC_PATH" ]]; then
    EXEC_PATH="$(find "$PUBLISH_DIR" -maxdepth 1 -type f -perm -111 2>/dev/null | head -n 1 || true)"
  fi
  [[ -n "${EXEC_PATH:-}" && -f "$EXEC_PATH" ]] || die "No se ha encontrado el ejecutable en $PUBLISH_DIR"
  EXEC_NAME="$(basename "$EXEC_PATH")"
  chmod +x "$EXEC_PATH" || true

  case "$RID" in
    linux-x64) DEB_ARCH="amd64" ;;
    linux-arm64) DEB_ARCH="arm64" ;;
    *) die "RID no soportado para .deb: $RID" ;;
  esac

  STAGING_DIR="$STAGING_ROOT/$RID"
  PKG_ROOT="$STAGING_DIR/root"
  DEBIAN_DIR="$PKG_ROOT/DEBIAN"
  INSTALL_DIR="$PKG_ROOT/opt/$APP_NAME"
  BIN_DIR="$PKG_ROOT/usr/bin"
  DESKTOP_DIR="$PKG_ROOT/usr/share/applications"
  ICON_DIR="$PKG_ROOT/usr/share/icons/hicolor/256x256/apps"

  rm -rf "$STAGING_DIR"
  mkdir -p "$DEBIAN_DIR" "$INSTALL_DIR" "$BIN_DIR" "$DESKTOP_DIR" "$ICON_DIR"

  echo "==> Copiando publish a $INSTALL_DIR"
  cp -R "$PUBLISH_DIR/"* "$INSTALL_DIR/"
  chmod +x "$INSTALL_DIR/$EXEC_NAME" || true

  echo "==> Creando launcher en /usr/bin/$APP_NAME"
  cat > "$BIN_DIR/$APP_NAME" <<EOF
#!/usr/bin/env bash
exec "/opt/$APP_NAME/$EXEC_NAME" "\$@"
EOF
  chmod 755 "$BIN_DIR/$APP_NAME"

  echo "==> Creando desktop entry"
  cat > "$DESKTOP_DIR/$APP_NAME.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=$APP_DISPLAY_NAME
Comment=$APP_DESCRIPTION
Exec=$APP_NAME
Icon=$APP_NAME
Terminal=false
Categories=Education;Science;
EOF
  chmod 644 "$DESKTOP_DIR/$APP_NAME.desktop"

  echo "==> Generando icono 256x256"
  ICON_OUT="$ICON_DIR/$APP_NAME.png"
  if command -v magick >/dev/null 2>&1; then
    magick "$ICON_SRC" -resize 256x256 "$ICON_OUT"
  elif command -v convert >/dev/null 2>&1; then
    convert "$ICON_SRC" -resize 256x256 "$ICON_OUT"
  else
    echo "WARN: No encuentro ImageMagick, se copia el icono sin redimensionar." >&2
    cp "$ICON_SRC" "$ICON_OUT"
  fi
  chmod 644 "$ICON_OUT"

  INSTALLED_SIZE="$(du -sk "$INSTALL_DIR" | awk '{print $1}')"
  cat > "$DEBIAN_DIR/control" <<EOF
Package: $PACKAGE_NAME
Version: $VERSION
Section: education
Priority: optional
Architecture: $DEB_ARCH
Maintainer: OfertaDemanda <noreply@local>
Installed-Size: $INSTALLED_SIZE
Homepage: https://github.com/joseantoniobouortells/OfertaDemandaAvalonia
Description: OfertaDemanda, simulador visual de microeconomía.
EOF

  DEB_PATH="$DEB_OUTPUT_DIR/${APP_NAME}_${VERSION}_${DEB_ARCH}.deb"
  echo "==> Empaquetando .deb: $DEB_PATH"
  dpkg-deb --build --root-owner-group "$PKG_ROOT" "$DEB_PATH" >/dev/null

  [[ -f "$DEB_PATH" ]] || die "No se generó el .deb: $DEB_PATH"
done

echo
echo "==> .deb listo en: $DEB_OUTPUT_DIR"
