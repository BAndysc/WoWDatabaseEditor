set -e
# Display installed .NET SDKs
dotnet --list-sdks

# Publish projects for Windows, macOS, and Linux
dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-win/ LoaderAvalonia/LoaderAvalonia.csproj -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=false

dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-mac/ LoaderAvalonia/LoaderAvalonia.csproj -r osx-x64

dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-linux/ LoaderAvalonia/LoaderAvalonia.csproj -r linux-x64 -p:PublishSingleFile=true -p:PublishTrimmed=false

dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-win/ Modules/CrashReport/CrashReport.csproj -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=false

dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-mac/ Modules/CrashReport/CrashReport.csproj -r osx-x64

dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-linux/ Modules/CrashReport/CrashReport.csproj -r linux-x64 -p:PublishSingleFile=true -p:PublishTrimmed=false

dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-win/ Updater/Updater.csproj -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=false

dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-mac/ Updater/Updater.csproj -r osx-x64

dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-linux/ Updater/Updater.csproj -r linux-x64 -p:PublishSingleFile=true -p:PublishTrimmed=false

dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-win/parser WoWPacketParserLoader -r win-x64

dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-mac/parser WoWPacketParserLoader -r osx-x64

dotnet publish -c Release --self-contained false -f net8.0 -o bin/wowdatabaseeditor-avalonia-linux/parser WoWPacketParserLoader -r linux-x64

# Rename operations for different platforms
mv bin/wowdatabaseeditor-avalonia-mac/Updater bin/wowdatabaseeditor-avalonia-mac/_Updater

mv bin/wowdatabaseeditor-avalonia-linux/Updater bin/wowdatabaseeditor-avalonia-linux/_Updater

mv bin/wowdatabaseeditor-avalonia-win/Updater.exe bin/wowdatabaseeditor-avalonia-win/_Updater.exe

mv bin/wowdatabaseeditor-avalonia-mac/WoWDatabaseEditor bin/wowdatabaseeditor-avalonia-mac/WoWDatabaseEditorCore.Avalonia

mv bin/wowdatabaseeditor-avalonia-linux/WoWDatabaseEditor bin/wowdatabaseeditor-avalonia-linux/WoWDatabaseEditorCore.Avalonia

mv bin/wowdatabaseeditor-avalonia-win/WoWDatabaseEditor.exe bin/wowdatabaseeditor-avalonia-win/WoWDatabaseEditorCore.Avalonia.exe

echo $APPVEYOR_PULL_REQUEST_NUMBER

if [ -z "$APPVEYOR_PULL_REQUEST_NUMBER" ]; then
    git clone -b avalonia11 https://$REPO_TOKEN@github.com/BAndysc/WoWDatabaseEditorExtras --recurse-submodules
    
    cd WoWDatabaseEditorExtras/WoWDatabaseEditor
    git checkout $APPVEYOR_REPO_COMMIT
    cd ..
    
    mkdir modules
    dotnet publish WDE.Github/WDE.Github.csproj -o modules/ -c Release -f net8.0
    
    cp modules/WDE.Github.dll ../bin/wowdatabaseeditor-avalonia-win/WDE.Github.dll
    cp modules/WDE.Github.dll ../bin/wowdatabaseeditor-avalonia-mac/WDE.Github.dll
    cp modules/WDE.Github.dll ../bin/wowdatabaseeditor-avalonia-linux/WDE.Github.dll
    
    cd ..
fi

# Cleanup operations in Windows bin directory
cd bin/wowdatabaseeditor-avalonia-win/
rm -rf *.pdb *.xml
rm -f AvaloniaStyles.exe AvaloniaStyles.deps.json AvaloniaStyles.runtimeconfig.json *.pdb Dock.* WoWDatabaseEditorCore.Avalonia.GUI.exe WoWDatabaseEditorCore.Avalonia.GUI.runtimeconfig.json WoWDatabaseEditorCore.Avalonia.GUI.deps.json WoWPacketParserLoader.exe WoWPacketParserLoader.deps.json
cd ../../

# Appending to app.ini for all platforms
for app_ini in bin/wowdatabaseeditor-avalonia-win/app.ini bin/wowdatabaseeditor-avalonia-mac/app.ini bin/wowdatabaseeditor-avalonia-linux/app.ini; do
{
echo "COMMIT=$APPVEYOR_REPO_COMMIT"
echo "BRANCH=$APPVEYOR_REPO_BRANCH"
echo "VERSION=$APPVEYOR_BUILD_VERSION"
echo "BUILD_VERSION=$APPVEYOR_BUILD_NUMBER"
echo "UPDATE_SERVER=$DEPLOY_URL"
echo "MARKETPLACE=default"
echo "MAPDATA_SERVER=$DEPLOY_URL"
} >> "$app_ini"
done

for fold in bin/wowdatabaseeditor-avalonia-win/parser #bin/wowdatabaseeditor-avalonia-mac/parser bin/wowdatabaseeditor-avalonia-linux/parser
do
  mkdir "$fold/Parsers"
  mv $fold/WowPacketParserModule.* $fold/Parsers
done

# Platform specific app.ini entries
echo "PLATFORM=Windows" >> bin/wowdatabaseeditor-avalonia-win/app.ini
echo "PLATFORM=MacOs" >> bin/wowdatabaseeditor-avalonia-mac/app.ini
echo "PLATFORM=Linux" >> bin/wowdatabaseeditor-avalonia-linux/app.ini

# Downloading and placing StormLib and CascLib
curl -fsSL -o bin/wowdatabaseeditor-avalonia-win/storm.dll https://github.com/BAndysc/StormCascLibBinary/releases/download/windows/StormLib.dll
curl -fsSL -o bin/wowdatabaseeditor-avalonia-win/casc.dll https://github.com/BAndysc/StormCascLibBinary/releases/download/windows/CascLib.dll
curl -fsSL -o bin/wowdatabaseeditor-avalonia-linux/libstorm.so https://github.com/BAndysc/StormCascLibBinary/releases/download/linux/libstorm.so
curl -fsSL -o bin/wowdatabaseeditor-avalonia-linux/libcasc.so https://github.com/BAndysc/StormCascLibBinary/releases/download/linux/libcasc.so

# Downloading SQLs for Windows and unpacking
mkdir -p bin/wowdatabaseeditor-avalonia-win/sqls
curl -fsSL -o bin/wowdatabaseeditor-avalonia-win/sqls/sqls.tar.gz https://github.com/BAndysc/sqls/releases/download/v0.2.22/sqls_0.2.22_Windows_x86_64.tar.gz
7z x bin/wowdatabaseeditor-avalonia-win/sqls/sqls.tar.gz -so | 7z x -si -ttar -obin/wowdatabaseeditor-avalonia-win/sqls/
rm -f bin/wowdatabaseeditor-avalonia-win/sqls/sqls.tar.gz bin/wowdatabaseeditor-avalonia-win/sqls/sqls.tar

# Mac OS Bundle and packaging
mkdir -p "WoW Database Editor.app/Contents/MacOS"
mkdir -p "WoW Database Editor.app/Contents/Resources"
cp WoWDatabaseEditorCore.Avalonia/Resources/Info.plist "WoW Database Editor.app/Contents"
cp WoWDatabaseEditorCore.Avalonia/Resources/icon.icns "WoW Database Editor.app/Contents/Resources"
mv bin/wowdatabaseeditor-avalonia-mac/* "WoW Database Editor.app/Contents/MacOS"

# Packaging with 7z
7z a WoWDatabaseEditorWindows.zip ./bin/wowdatabaseeditor-avalonia-win/*
7z a WoWDatabaseEditorMacOs.zip "WoW Database Editor.app/"
7z a WoWDatabaseEditorLinux.zip ./bin/wowdatabaseeditor-avalonia-linux/*
