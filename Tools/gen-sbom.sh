#!/bin/bash
# Generate CycloneDX format SBOM.
#
# Usage:
#   ./gen-sbom.sh [-o <output-directory>]
set -euo pipefail

COMMIT_HASH=$(git rev-parse HEAD)
COMMIT_ID=${COMMIT_HASH:0:12}

DATE=$(date '+%Y%m%d%H%M')

while read -r PROJECT
do
    FILE_NAME=$(basename "${PROJECT}")
    LIB="${FILE_NAME%.csproj}"
    IS_PKG=$(xmllint --xpath "string(//IsPackable)" "${PROJECT}")
    if [[ "${IS_PKG}" != "false" ]]; then
        SBOM_FILE="${LIB}-${COMMIT_ID}-${DATE}.sbom.json"
        dotnet-CycloneDX "${PROJECT}" -fn "${SBOM_FILE}" -ed -t -F Json "$@"
    fi
done < <(find . -name "*.csproj")
