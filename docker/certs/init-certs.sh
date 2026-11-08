#!/bin/sh

set -e
cd /certs
if [ -f server.crt ]; then
  echo "Certifikat already exists, skip."
  exit 0
fi

openssl ecparam -name prime256v1 -genkey -noout -out ca.key
openssl req -new -x509 -key ca.key -sha256 -days 3650 \
  -subj "/CN=IoT Platform Ca" -out ca.crt

openssl ecparam -name prime256v1 -genkey -noout -out server.key
openssl req -new -key server.key -subj "/CN=mosquitto" -out server.csr
printf "subjectAltName=%s\nextendedKeyUsage=serverAuth\n" "$TLS_SAN" >san.ext
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key -CAcreateserial \
  -sha256 -days 730 -extfile san.ext -out server.crt

rm -f server.csr san.ext
chown 1883:1883 server.key server.crt ca.crt
chmod 640 server.key
echo "Certifikat created"
