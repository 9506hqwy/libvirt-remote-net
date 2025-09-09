#!/bin/bash
set -euo pipefail

# install xmllint
sudo apt-get update -y
sudo apt-get install -y libxml2-utils

# define extra dependencies
LIB="Xdr"

# create local package directory
mkdir -p "${LOCALPKG}"

# doenload extra dependencies
while read -r PROJECT
do
    VERION=$(xmllint --xpath "string(//PackageReference[@Include='${LIB}']/@Version)" "${PROJECT}")
    if [ -n "${VERION}" ]; then
        FILE_NAME="${LIB}.${VERION}.nupkg"
        if [[ ! -f "${LOCALPKG}/${FILE_NAME}" ]]; then
            curl -fsSLO --output-dir "${LOCALPKG}" --url "https://github.com/9506hqwy/xdr-net/releases/download/${VERION}/${FILE_NAME}"
        fi
    fi
done < <(find . -name "*.csproj")
