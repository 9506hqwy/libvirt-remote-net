#!/bin/bash

set -eu

VERSION="v9.1.0"
PROTO_URL="https://raw.githubusercontent.com/libvirt/libvirt/${VERSION}/src/rpc/virnetprotocol.x"
REMOTE_URL="https://raw.githubusercontent.com/libvirt/libvirt/${VERSION}/src/remote/remote_protocol.x"

SHDIR=`cd $(dirname $0); pwd`

WORKDIR=`mktemp -d`
trap 'rm -rf ${WORKDIR}' EXIT

# Generate binding.
curl -sSL -o ${WORKDIR}/Protocol.x ${PROTO_URL}
cat - << EOF >> ${WORKDIR}/Protocol.x
const VIR_UUID_BUFLEN = 16;
EOF
dotnet rpc-gen ${WORKDIR}/Protocol.x
cp -f "${WORKDIR}/Protocol.cs" "${SHDIR}/../LibvirtRemote/Generated/"

curl -sSL -o ${WORKDIR}/Binding.x ${REMOTE_URL}
cat - << EOF >> ${WORKDIR}/Binding.x
const VIR_SECURITY_MODEL_BUFLEN = 256;
const VIR_SECURITY_LABEL_BUFLEN = 4096;
const VIR_SECURITY_DOI_BUFLEN = 256;
const VIR_UUID_BUFLEN = 16;
const VIR_TYPED_PARAM_INT = 1;
const VIR_TYPED_PARAM_UINT = 2;
const VIR_TYPED_PARAM_LLONG = 3;
const VIR_TYPED_PARAM_ULLONG = 4;
const VIR_TYPED_PARAM_DOUBLE = 5;
const VIR_TYPED_PARAM_BOOLEAN = 6;
const VIR_TYPED_PARAM_STRING = 7;
EOF
dotnet rpc-gen ${WORKDIR}/Binding.x
cp -f "${WORKDIR}/Binding.cs" "${SHDIR}/../LibvirtRemote/Generated/"

# Generate client.
dotnet run --project "${SHDIR}/Gen/Gen.csproj"
mv -f VirtClient.cs "${SHDIR}/../LibvirtRemote/Generated/"
mv -f VirtEvent.cs "${SHDIR}/../LibvirtRemote/Generated/"
