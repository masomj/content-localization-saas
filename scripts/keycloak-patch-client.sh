#!/bin/bash
# keycloak-patch-client.sh
# Patches the InterCopy-web client redirect URIs and web origins after deploy.
# Safe to run repeatedly — only updates specific fields, never touches users/roles/secrets.

set -euo pipefail

KCADM="/opt/keycloak/bin/kcadm.sh"
KC_CONTAINER="intercopy-keycloak-1"
REALM="InterCopy"
CLIENT_ID="InterCopy-web"

# Source .env for Keycloak admin password
source /opt/intercopy/.env

# Wait for Keycloak to be ready (up to 90s)
echo "Waiting for Keycloak to be ready..."
for i in $(seq 1 90); do
  if docker exec $KC_CONTAINER $KCADM config credentials \
    --server http://localhost:8080 --realm master \
    --user "${KEYCLOAK_ADMIN}" --password "${KEYCLOAK_ADMIN_PASSWORD}" 2>/dev/null; then
    echo "Keycloak ready after ${i}s"
    break
  fi
  if [ "$i" -eq 90 ]; then
    echo "ERROR: Keycloak not ready after 90s"
    exit 1
  fi
  sleep 1
done

# Find the client UUID
CLIENT_UUID=$(docker exec $KC_CONTAINER $KCADM get clients -r $REALM --fields clientId,id 2>/dev/null \
  | python3 -c "
import json,sys
for c in json.load(sys.stdin):
    if c['clientId']=='$CLIENT_ID':
        print(c['id'])
")

if [ -z "$CLIENT_UUID" ]; then
  echo "ERROR: Client '$CLIENT_ID' not found in realm '$REALM'"
  exit 1
fi

echo "Patching client $CLIENT_ID ($CLIENT_UUID)..."

# Patch redirect URIs and web origins
docker exec $KC_CONTAINER $KCADM update clients/$CLIENT_UUID -r $REALM \
  -s "redirectUris=[\"http://localhost:3000/*\",\"https://${DOMAIN}/*\"]" \
  -s "webOrigins=[\"http://localhost:3000\",\"https://${DOMAIN}\"]"

echo "Verifying..."
docker exec $KC_CONTAINER $KCADM get clients/$CLIENT_UUID -r $REALM \
  --fields redirectUris,webOrigins

echo "Keycloak client patched successfully."
