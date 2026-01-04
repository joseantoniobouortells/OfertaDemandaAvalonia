#!/usr/bin/env bash
set -euo pipefail

project_path="src/OfertaDemanda.Mobile/OfertaDemanda.Mobile.csproj"
framework="net10.0-ios"
bundle_id="com.joseantoniobouortells.ofertademanda.mobile"

udid="${1:-${SIM_UDID:-}}"

if [[ -z "${udid}" ]]; then
  udid="$(
    python3 - <<'PY'
import json
import sys
import subprocess
data = json.loads(subprocess.check_output(["xcrun", "simctl", "list", "devices", "--json"], text=True))
devices = data.get("devices", {})

def pick_booted():
    for runtime, entries in devices.items():
        for dev in entries:
            if dev.get("state") == "Booted":
                return dev.get("udid")
    return None

def pick_ios_262():
    for runtime, entries in devices.items():
        if "iOS-26-2" in runtime or "iOS 26.2" in runtime:
            for dev in entries:
                return dev.get("udid")
    return None

udid = pick_booted() or pick_ios_262()
if not udid:
    sys.exit(1)
print(udid)
PY
  )"
fi

if [[ -z "${udid}" ]]; then
  echo "No simulator UDID found. Pass one explicitly: scripts/run-ios-sim.sh <UDID>" >&2
  exit 1
fi

dotnet build "${project_path}" -f "${framework}"

app_path="src/OfertaDemanda.Mobile/bin/Debug/${framework}/iossimulator-arm64/OfertaDemanda.Mobile.app"

xcrun simctl boot "${udid}" >/dev/null 2>&1 || true
xcrun simctl install "${udid}" "${app_path}"
xcrun simctl launch --console --terminate-running-process "${udid}" "${bundle_id}"
