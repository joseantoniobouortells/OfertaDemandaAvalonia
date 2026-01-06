#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Uso: $(basename "$0") <version>" >&2
  exit 1
fi

NEW_VERSION="$1"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd -P)"

python3 - "$NEW_VERSION" "$ROOT_DIR" <<'PY'
import re
import sys
from pathlib import Path

new_version = sys.argv[1]
root = Path(sys.argv[2])

pattern = re.compile(r"(<Version>)([^<]+)(</Version>)")
paths = []
for base in (root / "src", root / "test"):
    if not base.exists():
        continue
    paths.extend(base.rglob("*.csproj"))

if not paths:
    print("No se han encontrado ficheros con <Version> en src/ o test/.", file=sys.stderr)
    sys.exit(1)

updated_any = False
for path in paths:
    text = path.read_text(encoding="utf-8")
    updated = pattern.sub(rf"\g<1>{new_version}\g<3>", text)
    if updated == text and "<Version>" not in text:
        marker = "<PropertyGroup>"
        idx = text.find(marker)
        if idx != -1:
            insert_at = idx + len(marker)
            updated = (
                text[:insert_at]
                + f"\n    <Version>{new_version}</Version>"
                + text[insert_at:]
            )
    if updated != text:
        path.write_text(updated, encoding="utf-8")
        print(f"Actualizado: {path}")
        updated_any = True

if not updated_any:
    print("No se han encontrado etiquetas <Version> en los .csproj.", file=sys.stderr)
    sys.exit(1)
PY
