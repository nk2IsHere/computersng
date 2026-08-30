#!/bin/sh
# Packs the game and SMAPI assemblies CI needs into a versioned game-refs zip.
# Also pins the versions in game-refs.version, which CI checks against the hosted zip.
# Usage: ./PackGameRefs.sh <GamePath>
set -e

GAME_PATH="${1:?Usage: ./PackGameRefs.sh <GamePath>}"
STAGE="$(mktemp -d)"

GAME_VERSION="$(grep -o '"Stardew Valley/[0-9][^"]*"' "$GAME_PATH/Stardew Valley.deps.json" | head -1 | cut -d/ -f2 | tr -d '"')"
SMAPI_VERSION="$(python3 - "$GAME_PATH/StardewModdingAPI.dll" <<'EOF'
import re, sys
data = open(sys.argv[1], "rb").read()
informational = re.findall(rb'[0-9]{1,2}\.[0-9]{1,2}\.[0-9]{1,3}\+[A-Za-z0-9.-]+', data)
print(informational[0].decode().split("+")[0] if informational else "")
EOF
)"

[ -n "$GAME_VERSION" ] || { echo "Could not read the game version from Stardew Valley.deps.json"; exit 1; }
[ -n "$SMAPI_VERSION" ] || { echo "Could not read the SMAPI version from StardewModdingAPI.dll"; exit 1; }

OUT="$(pwd)/game-refs-$GAME_VERSION-smapi-$SMAPI_VERSION.zip"

mkdir -p "$STAGE/smapi-internal"
for file in \
    "Stardew Valley.dll" \
    "Stardew Valley.deps.json" \
    "StardewValley.GameData.dll" \
    "MonoGame.Framework.dll" \
    "xTile.dll" \
    "BmFont.dll" \
    "StardewModdingAPI.dll" \
    "StardewModdingAPI.deps.json"
do
    cp "$GAME_PATH/$file" "$STAGE/" 2>/dev/null || echo "skipped missing $file"
done
cp "$GAME_PATH/smapi-internal/SMAPI.Toolkit.dll" "$STAGE/smapi-internal/"
cp "$GAME_PATH/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll" "$STAGE/smapi-internal/"

printf '{"game": "%s", "smapi": "%s"}\n' "$GAME_VERSION" "$SMAPI_VERSION" > "$STAGE/versions.json"
printf '{"game": "%s", "smapi": "%s"}\n' "$GAME_VERSION" "$SMAPI_VERSION" > "$(pwd)/game-refs.version"

rm -f "$OUT"
(cd "$STAGE" && zip -qr "$OUT" .)
rm -rf "$STAGE"

echo "Packed $OUT"
echo "Pinned game $GAME_VERSION and SMAPI $SMAPI_VERSION in game-refs.version"
echo "Upload the zip to the GAME_REFS_URL folder and commit game-refs.version"
