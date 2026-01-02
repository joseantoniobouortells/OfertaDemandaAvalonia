#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

default_project="src/OfertaDemanda.Desktop/OfertaDemanda.Desktop.csproj"
project_path_abs="${repo_root}/${default_project}"
if [[ ! -f "${project_path_abs}" ]]; then
    project_path_abs="$(find "${repo_root}/src" -name "*.csproj" | grep -Ei '(desktop|avalonia)' | head -n 1 || true)"
fi

if [[ -z "${project_path_abs}" || ! -f "${project_path_abs}" ]]; then
    echo "No se encontro un proyecto de escritorio bajo src/."
    exit 1
fi

if [[ "${project_path_abs}" != "${repo_root}/"* ]]; then
    echo "Ruta de proyecto fuera del repositorio: ${project_path_abs}"
    exit 1
fi

project_path_rel="${project_path_abs#${repo_root}/}"
tfm_line="$(grep -E "<TargetFramework>" "${project_path_abs}" | head -n 1 || true)"
tfm="$(echo "${tfm_line}" | sed -E 's/.*<TargetFramework>([^<]+).*/\1/')"
dotnet_version="8.0"
if [[ "${tfm}" =~ ^net([0-9]+)\.([0-9]+) ]]; then
    dotnet_version="${BASH_REMATCH[1]}.${BASH_REMATCH[2]}"
fi

dockerfile_path="${repo_root}/build/docker/Dockerfile.win-publish"
image_tag="oferta-demanda-win-publish:local"
output_dir="${repo_root}/artifacts/publish/win-x64"

echo "==> Proyecto: ${project_path_rel}"
echo "==> .NET SDK: ${dotnet_version}"
echo "==> Construyendo imagen Docker..."
docker build -f "${dockerfile_path}" \
    --build-arg "PROJECT_PATH=${project_path_rel}" \
    --build-arg "DOTNET_VERSION=${dotnet_version}" \
    -t "${image_tag}" \
    "${repo_root}"

echo "==> Limpiando salida local..."
rm -rf "${output_dir}"
mkdir -p "${output_dir}"

echo "==> Copiando publish desde el contenedor..."
container_id="$(docker create "${image_tag}")"
docker cp "${container_id}:/out/publish/win-x64/." "${output_dir}/"
docker rm "${container_id}" >/dev/null

echo "==> Publicacion completada en: ${output_dir}"
