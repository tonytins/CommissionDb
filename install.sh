#!/bin/sh

BUILD_MODE=${BUILD_MODE:-release} # Override with BUILD_MODE=debug ./install.sh
PROGRAM=commis

BUILD_DIR="$PWD/.build/$BUILD_MODE"
EXEC_PATH="$BUILD_DIR/$PROGRAM"
BIN_DIR="$HOME/.local/bin"
INSTALL_PATH="$BIN_DIR/$PROGRAM"

if [ ! -x "$EXEC_PATH" ]; then
    swift build -c "$BUILD_MODE" --product "$PROGRAM"
fi

if [ ! -d "$BIN_DIR" ]; then
    echo "Creating local bin directory."
    mkdir -p "$BIN_DIR"
fi

install -m 755 "$EXEC_PATH" "$INSTALL_PATH"

echo "Installed $PROGRAM to $INSTALL_PATH"
