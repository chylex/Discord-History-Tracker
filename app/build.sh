#!/bin/bash
set -e

if [ ! -f "DiscordHistoryTracker.sln" ]; then
	echo "Missing DiscordHistoryTracker.sln in working directory!"
	exit 1
fi

makezip() {
	pushd "./bin/$1"
	zip -9 -r "../$1.zip" .
	popd
}

rm -rf "./bin"

configurations=(win-x64 linux-x64 osx-x64)

for cfg in ${configurations[@]}; do
	dotnet publish Desktop -c Release -r "$cfg" -o "./bin/$cfg" -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=false -p:PublishTrimmed=true -p:TrimMode=partial -p:JsonSerializerIsReflectionEnabledByDefault=true --self-contained true
	makezip "$cfg"
done

dotnet publish Desktop -c Release -o "./bin/portable" --self-contained false
makezip "portable"
