#!/bin/bash

# Launch SMAPI in developer mode and auto-load a save (default: "dev").
# Usage: ./RunDev.sh <GAME_PATH> [SAVE_NAME]

GAME_PATH=$1
DEV_SAVE=${2:-dev}

if [ -d "$GAME_PATH" ]; then
    echo "Launching SMAPI from $GAME_PATH (autoloading save '$DEV_SAVE')"
    cd "$GAME_PATH" || exit 1
    SMAPI_DEVELOPER_MODE=true COMPUTERS_DEV_LOAD_SAVE="$DEV_SAVE" ./StardewModdingAPI
else
    echo "Game path not found"
fi
