#!/usr/bin/env bash
# One command to host Bait & Switch on Edgegap. No Docker needed. You only need your Edgegap API token.
#
#   1. Unity: Tools > Bait & Switch > Build Linux Server            (makes Builds/Server)
#   2. Edgegap dashboard: bottom-left account menu > User Settings > Tokens > Create token, copy it
#   3. Git Bash, in the project folder:   bash tools/deploy/deploy_edgegap.sh
#      (paste the token when asked; it is never written to disk or the repo)
#
# What it does: packs Builds/Server into a container image, pushes it to your Edgegap registry (credentials fetched
# with the token), creates/updates app version "prod" (UDP 7777 named "gameport"), stores the token as the broker's
# secret and redeploys the broker so "Dive in" starts a server on demand.
# Optional: SMOKE=1 asks the broker for a server and waits until it is ready.   APP=... to use another app name.
set -euo pipefail

APP=${APP:-BaitAndSwitch}            # the application you created in the Edgegap dashboard
VERSION=${1:-prod}
IMAGE_NAME=${IMAGE_NAME:-bait-and-switch}
TAG=${TAG:-$(date +%Y%m%d-%H%M%S)}
TOOLS=${BAIT_TOOLS:-/c/Users/soham/bait-tools}
CRANE="$TOOLS/crane/crane.exe"
SECRET_FILE=${SERVER_SECRET_FILE:-$TOOLS/server_secret.txt}
CA=${CA_BUNDLE:-$(cygpath -m /mingw64/etc/ssl/certs/ca-bundle.crt)}
BROKER_URL=$(tr -d '\r\n' < worker/broker_url.txt)
API=https://api.edgegap.com
WORK=$(cygpath -m "$(mktemp -d)")   # Windows-style path: native crane/curl/python all understand it
trap 'rm -rf "$WORK"' EXIT

if [ -z "${EDGEGAP_TOKEN:-}" ]; then
  read -r -s -p "Paste your Edgegap API token (hidden), then Enter: " EDGEGAP_TOKEN; echo
fi
[ -n "$EDGEGAP_TOKEN" ] || { echo "No token given."; exit 1; }
AUTH="$EDGEGAP_TOKEN"; case "$AUTH" in [Tt]oken\ *) ;; *) AUTH="token $AUTH";; esac

[ -f Builds/Server/BaitSwitchServer.x86_64 ] || { echo "No server build. In Unity: Tools > Bait & Switch > Build Linux Server."; exit 1; }
[ -f "$SECRET_FILE" ] || { echo "Missing $SECRET_FILE (the broker SERVER_SECRET)."; exit 1; }

echo "== 0/5 checking the token"
code=$(curl -s -o "$WORK/app.json" -w '%{http_code}' -H "Authorization: $AUTH" "$API/v1/app/$APP")
if [ "$code" = "401" ] || [ "$code" = "403" ]; then echo "Edgegap rejected the token ($code). Create a new one under User Settings > Tokens."; exit 1; fi
if [ "$code" = "404" ]; then
  curl -sf -H "Authorization: $AUTH" -H "Content-Type: application/json" \
       -d "{\"name\":\"$APP\",\"is_active\":true,\"image\":\"\"}" "$API/v1/app" > /dev/null
  echo "created app $APP"
elif [ "$code" != "200" ]; then echo "Edgegap API said $code for app $APP:"; cat "$WORK/app.json"; exit 1; fi
echo "app $APP ok"

echo "== 1/5 registry credentials"
# from the dashboard (Service > Container Registry > request credentials): EG_PROJECT, EG_REG_USER, EG_REG_PASS
if [ -n "${EG_PROJECT:-}" ] && [ -n "${EG_REG_USER:-}" ] && [ -n "${EG_REG_PASS:-}" ]; then
  REG=${EG_REGISTRY:-registry.edgegap.com}
elif curl -sf -H "Authorization: $AUTH" "$API/v1/wizard/registry-credentials" > "$WORK/reg.json"; then
  read -r REG EG_PROJECT EG_REG_USER EG_REG_PASS < <(python -I -c "
import json,sys
d=json.load(open(sys.argv[1]))
print((d.get('registry_url') or 'registry.edgegap.com').replace('https://','').strip('/'), d['project'], d['username'], d['token'])
" "$WORK/reg.json")
else
  echo "Need the Container Registry credentials: Edgegap dashboard > Service > Container Registry > request credentials,"
  echo "then rerun with EG_PROJECT=<project> EG_REG_USER=<username> EG_REG_PASS=<token> set."
  exit 1
fi
IMAGE="$REG/$EG_PROJECT/$IMAGE_NAME"
echo "registry $REG/$EG_PROJECT"

echo "== 2/5 packing the server"
python -I tools/deploy/make_layer.py Builds/Server "$CA" "$WORK/layer.tar" Builds/WebGL

echo "== 3/5 pushing $IMAGE:$TAG (takes a minute or two)"
printf '%s' "$EG_REG_PASS" | "$CRANE" auth login "$REG" -u "$EG_REG_USER" --password-stdin
"$CRANE" append --platform linux/amd64 -b ubuntu:22.04 -f "$WORK/layer.tar" -t "$IMAGE:$TAG"
MSYS_NO_PATHCONV=1 "$CRANE" mutate "$IMAGE:$TAG" -t "$IMAGE:$TAG" \
  --entrypoint=/app/BaitSwitchServer.x86_64 \
  --cmd=-batchmode,-nographics,-logFile,/dev/stdout \
  -w /app --exposed-ports 7777/udp

echo "== 4/5 app version $VERSION"
VERSION="$VERSION" TAG="$TAG" REG="$REG" IMG="$EG_PROJECT/$IMAGE_NAME" BROKER_URL="$BROKER_URL" \
REG_USER="$EG_REG_USER" REG_PASS="$EG_REG_PASS" SECRET_FILE="$SECRET_FILE" OUT="$WORK/version.json" python -I - <<'PY'
import json, os
e = os.environ
body = {
    "name": e["VERSION"], "is_active": True,
    "docker_repository": e["REG"], "docker_image": e["IMG"], "docker_tag": e["TAG"],
    "private_username": e["REG_USER"], "private_token": e["REG_PASS"],
    "req_cpu": 1024, "req_memory": 2048,   # inside the free tier (1.5 vCPU / 3 GB)
    "max_duration": 60, "time_to_deploy": 60, "termination_grace_period_seconds": 5,
    "restart_policy": "never", "force_cache": False, "verify_image": False,
    "ports": [
        {"port": 7777, "protocol": "UDP", "to_check": False, "tls_upgrade": False, "name": "gameport"},   # Windows clients (KCP)
        {"port": 7778, "protocol": "WS", "to_check": False, "tls_upgrade": True, "name": "ws"},           # browser clients (wss)
        {"port": 8080, "protocol": "HTTP", "to_check": False, "tls_upgrade": True, "name": "web"},        # the WebGL page (https)
    ],
    "envs": [
        {"key": "BROKER_URL", "value": e["BROKER_URL"], "is_secret": False},
        {"key": "BROKER_SECRET", "value": open(e["SECRET_FILE"]).read().strip(), "is_secret": True},
    ],
}
open(e["OUT"], "w").write(json.dumps(body))
PY
code=$(curl -s -o "$WORK/resp.json" -w '%{http_code}' -H "Authorization: $AUTH" -H "Content-Type: application/json" \
       -d @"$WORK/version.json" "$API/v1/app/$APP/version")
if [ "$code" != "200" ] && [ "$code" != "201" ]; then
  code=$(curl -s -o "$WORK/resp.json" -w '%{http_code}' -X PATCH -H "Authorization: $AUTH" -H "Content-Type: application/json" \
         -d @"$WORK/version.json" "$API/v1/app/$APP/version/$VERSION")
fi
[ "$code" = "200" ] || [ "$code" = "201" ] || { echo "Version create/update failed ($code):"; cat "$WORK/resp.json"; exit 1; }
echo "version $VERSION -> $TAG"

echo "== 5/5 broker: token secret + redeploy (app $APP, version $VERSION)"
export PATH="$TOOLS/node-v22.11.0-win-x64:$PATH"
(cd worker && printf '%s' "$EDGEGAP_TOKEN" | npx --yes wrangler secret put EDGEGAP_TOKEN > /dev/null && npx --yes wrangler deploy > /dev/null) \
  && echo "broker updated"

if [ "${SMOKE:-0}" = "1" ]; then
  echo "== smoke: asking the broker for a sea"
  curl -s -X POST "$BROKER_URL/join" -H "Content-Type: application/json" -d '{}'; echo
  for i in $(seq 1 40); do
    s=$(curl -s "$BROKER_URL/status"); echo "$s"
    case "$s" in *'"state":"ready"'*) echo "READY"; exit 0;; *'"state":"error"'*) exit 1;; esac
    sleep 5
  done
  echo "not ready after 200 s"; exit 1
fi
echo "done. Press Dive in: the first sea takes ~30-60 s to wake."
