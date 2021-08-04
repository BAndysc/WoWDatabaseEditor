# WoW Database Editor
Integrated development environment (IDE) for Smart Scripts and database editing for TrinityCore based servers.

This project is continuation of [Visual SAI Studio](https://github.com/BandyscLegacy/VisualSAIStudio), but rewritten from scratch. Anyone is welcome to help :-) Together we can make great wow emu editor! **Smart script editing is already in good enough shape to use it on daily basis** 

(screenshots below)

# Supported server versions

 * Supported database: TC 3.3.5, TC 4.3.4 ("preservation project"), TC master (9.0.x), AzerothCore (3.3.5)
 * Supported DBC: 3.3.5, 4.3.4 
 
 (9.0.x dbc support in progress)

# Download latest version

Application has built in auto updater, so you do not have to redownload here to upgrade the app.

## Mac OS / Linux / Windows version
**To run the cross platform version, you need [.NET 5 Runtime](https://dotnet.microsoft.com/download/dotnet/5.0) for your OS**.

WoW Database Editor is compatibile with both MacOS, Linux and Windows.

**Download links**: 
 * [WoWDatabaseEditor for Windows.zip](https://ci.appveyor.com/api/projects/BAndysc/wowdatabaseeditor/artifacts/WoWDatabaseEditorWindows.zip?branch=master)
 * [WoWDatabaseEditor for Linux.zip](https://github.com/BAndysc/WoWDatabaseEditor/releases) (please use version from GitHub Releases and update via application)
 * [WoWDatabaseEditor for MacOS.zip](https://github.com/BAndysc/WoWDatabaseEditor/releases) (please use version from GitHub Releases and update via application)

# I want to contribute!
That's a fantastic news! There is still a lot to do in the IDE, if you do not know what you can do, check out [opened issues, especially those marked as "help wanted"](https://github.com/BAndysc/WoWDatabaseEditor/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22)

# Sponsors

WoW Database Editor is sponsored by [Atlantiss](https://atlantiss.org/) and [Tauri](https://tauriwow.com/).

![Atlantiss & Tauri](https://i.imgur.com/gj0HPAk.png)

# How to build

WoW Database Editor is using [git submodules](https://git-scm.com/book/en/v2/Git-Tools-Submodules), therefore after you clone, after you pull you have to download submodules:

```
git submodule update --init --recursive
```

Now you can open solution in Visual Studio or other C#/.NET IDE and build. Start "WoWDatabaseCore.Avalonia" project

**To build version to distribute:**

```
-- Windows version
dotnet publish -c Release --self-contained false -f net5.0 -o bin/wowdatabaseeditor-avalonia-win/ LoaderAvalonia/LoaderAvalonia.csproj -r win7-x64

-- MacOS version
dotnet publish -c Release --self-contained false -f net5.0 -o bin/wowdatabaseeditor-avalonia-mac/ LoaderAvalonia/LoaderAvalonia.csproj -r osx-x64

-- Linux version
dotnet publish -c Release --self-contained false -f net5.0 -o bin/wowdatabaseeditor-avalonia-linux/ LoaderAvalonia/LoaderAvalonia.csproj -r linux-x64
```


## Thanks to:
 * [tgjones](https://github.com/tgjones/gemini) for Gemini Graph Editor
 * WDBXEditor for dbc loading
 * Atlantiss - the editor begin as internal tool
 * TrinityCore, Cmangos and everyone who contributes to WoW Core!
 * [Avalonia](https://avaloniaui.net/) - WoW Database Editor is built upon Avalonia UI framework

![screenshot7](https://raw.githubusercontent.com/BAndysc/WoWDatabaseEditor/master/Examples/screenshot7.png)
![screenshot4](https://raw.githubusercontent.com/BAndysc/WoWDatabaseEditor/master/Examples/screenshot4_macos.png)
![screenshot5](https://raw.githubusercontent.com/BAndysc/WoWDatabaseEditor/master/Examples/screenshot6.png)
![screenshot6](https://raw.githubusercontent.com/BAndysc/WoWDatabaseEditor/master/Examples/screenshot5_macos.png)
![screenshot1](https://github.com/BAndysc/WoWDatabaseEditor/blob/680b3d10896fe017f9731c1b114df7b43308ae8f/Examples/screenshot2.png?raw=true)
![screenshot3](https://github.com/BAndysc/WoWDatabaseEditor/blob/680b3d10896fe017f9731c1b114df7b43308ae8f/Examples/screenshot3.png?raw=true)
![darktheme](https://raw.githubusercontent.com/BAndysc/WoWDatabaseEditor/master/Examples/darktheme.png)
