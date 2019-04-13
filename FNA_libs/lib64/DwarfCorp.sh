#!/bin/bash
# MonoKickstart Shell Script
# Written by Ethan "flibitijibibo" Lee

SCRIPT=$(readlink -f $0)
DIR=`dirname "$0"`
# Move to script's directory
cd "$DIR"

# Get the system architecture
UNAME=`uname`
ARCH=`uname -m`

# MonoKickstart picks the right libfolder, so just execute the right binary.
if [ "$UNAME" == "Darwin" ]; then
	# ... Except on OSX.
	export DYLD_LIBRARY_PATH=$DYLD_LIBRARY_PATH:./osx/

	# El Capitan is a total idiot and wipes this variable out, making the
	# Steam overlay disappear. This sidesteps "System Integrity Protection"
	# and resets the variable with Valve's own variable (they provided this
	# fix by the way, thanks Valve!). Note that you will need to update your
	# launch configuration to the script location, NOT just the app location
	# (i.e. Kick.app/Contents/MacOS/Kick, not just Kick.app).
	# -flibit
	if [ "$STEAM_DYLD_INSERT_LIBRARIES" != "" ] && [ "$DYLD_INSERT_LIBRARIES" == "" ]; then
		export DYLD_INSERT_LIBRARIES="$STEAM_DYLD_INSERT_LIBRARIES"
	fi
	./DwarfCorpFNA.bin.osx $@
else
    echo "Local dir is $DIR"
    chmod a+rwx ./DwarfCorpFNA.bin.x86_64
    export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$DIR
    if [ "$ARCH" == "x86_64" ]; then
            chmod a+rwx ./DwarfCorpFNA.bin.x86_64
	    LD_PRELOAD="/usr/lib/libSDL2-2.0.so.0" ./DwarfCorpFNA.bin.x86_64 $@
    else
        chmod a+rwx ./DwarfCorpFNA.bin.x86
	LD_PRELOAD="/usr/lib/libSDL2-2.0.so.0" ./DwarfCorpFNA.bin.x86 $@
    fi
fi
