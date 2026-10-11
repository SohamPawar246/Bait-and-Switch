#!/usr/bin/env bash
# Upload the Windows build to itch.io. Run from the repo root in Git Bash after Build Windows Client.
#   export BUTLER_API_KEY='...'          # https://itch.io/user/settings/api-keys (never commit it)
#   bash tools/deploy/push_itch.sh <itch-user>/<game-slug>
set -euo pipefail
: "${BUTLER_API_KEY:?export BUTLER_API_KEY first}"
TARGET=${1:?usage: push_itch.sh <itch-user>/<game-slug>}
BUTLER=${BUTLER:-/c/Users/soham/bait-tools/butler/butler.exe}
[ -f Builds/Windows/BaitAndSwitch.exe ] || { echo "No client build. Run Tools > Bait & Switch > Build Windows Client."; exit 1; }
# push the folder (butler diffs it); the debug-symbol folder is excluded
"$BUTLER" push Builds/Windows "$TARGET:windows" --ignore '*DoNotShip*' --userversion "$(date +%Y.%m.%d-%H%M)"
"$BUTLER" status "$TARGET:windows"
