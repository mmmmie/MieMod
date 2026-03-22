#!/bin/bash
set -euo pipefail
set -x

disable_remote_debug=false
for arg in "$@"; do
  case "$arg" in
    -nd)
      disable_remote_debug=true
      ;;
  esac
done

dotnet build -c Release MieMod.csproj
pkill -SIGTERM -f 'Slay the Spire 2' || true
pkill -SIGKILL -f 'Slay the Spire 2' || true
# cp -r ~/Library/Application\ Support/Steam/steamapps/common/Slay\ the\ Spire\ 2/SlayTheSpire2.app/Contents/MacOS/mods/ ~/Desktop/SlayTheSpire2_copy.app/Contents/MacOS/mods/
if [ "$disable_remote_debug" = true ]; then
  open -n ~/Library/Application\ Support/Steam/steamapps/common/Slay\ the\ Spire\ 2/SlayTheSpire2.app --args --force-steam off
else
  open -n ~/Library/Application\ Support/Steam/steamapps/common/Slay\ the\ Spire\ 2/SlayTheSpire2.app --args --force-steam off --remote-debug tcp://127.0.0.1:6007
fi
