#!/usr/bin/env bash
# Refreshes src/NexusMods.Networking.NexusWebApi/games.json, the game id <-> domain
# mapping embedded into the app. Only the fields the app reads (id, domain_name) are kept.
#
# The build used to download this file from Nexus Mods on every clean build; it is now
# committed so the build doesn't depend on a third-party server. Run this occasionally
# to pick up newly added games, then commit the result.
set -euo pipefail

url="${GAMES_JSON_URL:-https://data.nexusmods.com/file/nexus-data/games.json}"
out="$(dirname "$0")/../src/NexusMods.Networking.NexusWebApi/games.json"
tmp="$(mktemp)"
trap 'rm -f "$tmp"' EXIT

curl -fsSL -o "$tmp" "$url"
python3 -I - "$tmp" "$out" <<'PY'
import json, sys
src, dst = sys.argv[1], sys.argv[2]
games = json.load(open(src, encoding="utf-8"))
trimmed = sorted(({"id": g["id"], "domain_name": g["domain_name"]} for g in games), key=lambda g: g["id"])
if not trimmed:
    sys.exit("downloaded games.json is empty, refusing to overwrite")
with open(dst, "w", encoding="utf-8", newline="\n") as f:
    f.write("[\n")
    f.write(",\n".join(json.dumps(g, separators=(",", ":")) for g in trimmed))
    f.write("\n]\n")
print(f"wrote {len(trimmed)} games to {dst}")
PY
