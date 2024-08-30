#!/bin/bash

# copy assets/Library to $(GAME_PATH)/Computers/assets/Library with overwrite

GAME_PATH=$1

if [ -d $GAME_PATH ]; then
    echo "Copying Library to $GAME_PATH/Mods/Computers/assets"
    rsync -av --delete Computers/assets/Library/ $GAME_PATH/Mods/Computers/assets/Library
else
    echo "Game path not found"
fi
