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
import subprocess
import sys

def run(*args):
    return subprocess.check_output(args, text=True).strip()

devices_json = json.loads(run("xcrun", "simctl", "list", "devices", "--json"))
runtimes_json = json.loads(run("xcrun", "simctl", "list", "runtimes", "--json"))
types_json = json.loads(run("xcrun", "simctl", "list", "devicetypes", "--json"))

def parse_version(value):
    parts = []
    for chunk in value.split("."):
        try:
            parts.append(int(chunk))
        except ValueError:
            parts.append(0)
    return tuple(parts)

runtimes = [
    runtime for runtime in runtimes_json.get("runtimes", [])
    if runtime.get("isAvailable") and runtime.get("name", "").startswith("iOS")
]

if not runtimes:
    sys.exit("No iOS runtimes found.")

runtimes.sort(key=lambda r: parse_version(r.get("version", "0")), reverse=True)
runtime = runtimes[0]
runtime_id = runtime["identifier"]

preferred_names = [
    "iPhone 16 Pro",
    "iPhone 16 Pro Max",
    "iPhone 16",
    "iPhone 15 Pro",
    "iPhone 15 Pro Max",
    "iPhone 15",
    "iPhone 14 Pro",
    "iPhone 14"
]

devices = devices_json.get("devices", {}).get(runtime_id, [])
iphone_devices = [dev for dev in devices if "iPhone" in dev.get("name", "")]

def pick_device():
    for name in preferred_names:
        for dev in iphone_devices:
            if dev.get("name") == name:
                return dev
    return iphone_devices[0] if iphone_devices else None

device = pick_device()
if device:
    print(device["udid"])
    sys.exit(0)

device_types = types_json.get("devicetypes", [])
iphone_types = [dev for dev in device_types if "iPhone" in dev.get("name", "")]

def pick_type():
    for name in preferred_names:
        for dev in iphone_types:
            if dev.get("name") == name:
                return dev
    return iphone_types[0] if iphone_types else None

device_type = pick_type()
if not device_type:
    sys.exit("No iPhone device types found.")

device_name = f"{device_type['name']} ({runtime['name']})"
udid = run("xcrun", "simctl", "create", device_name, device_type["identifier"], runtime_id)
print(udid)
PY
  )"
fi

if [[ -z "${udid}" ]]; then
  echo "No simulator UDID found. Pass one explicitly: scripts/run-ios-iphone.sh <UDID>" >&2
  exit 1
fi

dotnet build "${project_path}" -f "${framework}"

app_path="src/OfertaDemanda.Mobile/bin/Debug/${framework}/iossimulator-arm64/OfertaDemanda.Mobile.app"

xcrun simctl boot "${udid}" >/dev/null 2>&1 || true
open -a Simulator >/dev/null 2>&1 || true
for _ in {1..30}; do
  state="$(python3 - <<PY
import json
import subprocess

udid = "${udid}"
data = json.loads(subprocess.check_output(["xcrun", "simctl", "list", "devices", "--json"], text=True))
for runtime, devices in data.get("devices", {}).items():
    for dev in devices:
        if dev.get("udid") == udid:
            print(dev.get("state", ""))
            raise SystemExit(0)
print("")
PY
)"
  if [[ "${state}" == "Booted" ]]; then
    break
  fi
  sleep 1
done

xcrun simctl install "${udid}" "${app_path}"
xcrun simctl launch --console --terminate-running-process "${udid}" "${bundle_id}"
