@echo off
set list=win-x64 linux-x64 osx-x64

rmdir /S /Q bin

(for %%a in (%list%) do (
  dotnet publish Desktop -c Release -r %%a -o ./bin/%%a -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=false -p:PublishTrimmed=true -p:TrimMode=partial -p:JsonSerializerIsReflectionEnabledByDefault=true --self-contained true
  powershell "Compress-Archive -Path ./bin/%%a/* -DestinationPath ./bin/%%a.zip -CompressionLevel Optimal"
))

dotnet publish Desktop -c Release -o ./bin/portable --self-contained false
powershell "Compress-Archive -Path ./bin/portable/* -DestinationPath ./bin/portable.zip -CompressionLevel Optimal"

echo Done
pause
