#!/bin/bash
set -e

export TZ=UTC

if [ ! -f "DiscordHistoryTracker.sln" ]; then
  echo "Missing DiscordHistoryTracker.sln in working directory!"
  exit 1
fi

makezip() {
  TMP_PATH="/tmp/dht-build"
  BIN_PATH="$(pwd)/bin"

  rm -rf "$TMP_PATH"
  cp -r "$BIN_PATH/$1/" "$TMP_PATH"
  pushd "$TMP_PATH"

  find . -type d -exec chmod 755 {} \;
  find . -type f -exec chmod 644 {} \;

  chmod -f 755 DiscordHistoryTracker || true
  chmod -f 755 DiscordHistoryTracker.exe || true

  find . -type f | sort | zip -9 -X "$BIN_PATH/$1.zip" -@

  popd
  rm -rf "$TMP_PATH"
}

rm -rf "./bin"

configurations=(win-x64 linux-x64 osx-x64)

for cfg in "${configurations[@]}"; do
  "/mnt/c/Program Files/dotnet/dotnet.exe" publish Desktop -c Release -r "$cfg" -o "./bin/$cfg" --self-contained true
  makezip "$cfg"
done

"/mnt/c/Program Files/dotnet/dotnet.exe" publish Desktop -c Release -o "./bin/portable" -p:PublishSingleFile=false -p:PublishTrimmed=false --self-contained false
rm "./bin/portable/DiscordHistoryTracker.exe"
makezip "portable"
