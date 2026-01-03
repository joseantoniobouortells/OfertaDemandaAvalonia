#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

dockerfile_path="${repo_root}/build/docker/Dockerfile.linux-deb"
image_tag="oferta-demanda-linux-deb:local"
output_dir="${repo_root}/artifacts/linux/deb"

echo "==> Construyendo imagen Docker..."
docker build -f "${dockerfile_path}" \
  -t "${image_tag}" \
  "${repo_root}"

echo "==> Limpiando salida local..."
rm -rf "${output_dir}"
mkdir -p "${output_dir}"

echo "==> Copiando .deb desde el contenedor..."
container_id="$(docker create "${image_tag}")"
docker cp "${container_id}:/out/deb/." "${output_dir}/"
docker rm "${container_id}" >/dev/null

if ! ls "${output_dir}"/*.deb >/dev/null 2>&1; then
  echo "No se genero ningun .deb en ${output_dir}"
  exit 1
fi

echo "==> .deb listo en: ${output_dir}"
