# WoW Database Editor
Editor for Smart Scripts for TrinityCore based servers.

This project is continuation of [Visual SAI Studio](https://github.com/BandyscLegacy/VisualSAIStudio), but rewritten from scratch. It is still in early stages, but anyone is welcome to help :-) Together we can make great wow emu editor!

### Repositories

This application is splited into few repositories, at the moment:
 
 * [WoWDatabaseEditor.Common](https://github.com/BAndysc/WoWDatabaseEditor.Common) - contains general common projects like dbc loading, database connection, possibly mpq loading in the future
 * [WowDatabaseEditor](https://github.com/BAndysc/WoWDatabaseEditor) - contains projects directly connected to database editing like smart script editing
 
**Remember to clone this repo with submodules or else it won't compile!**

If folder `WoWDatabaseEditor.Common` is empty, you don't have submodules. To download submodules, use commands:

```
git submodule init
git submodule update
```

Later, `git submodule update` is enough to update sub repo

## Thanks to:
 * [tgjones](https://github.com/tgjones/gemini) for Gemini Graph Editor
 * WDBXEditor for dbc loading
 * Atlantiss - the editor begin as internal tool
 * TrinityCore, Cmangos and everyone who contributes to WoW Core!

![darktheme](https://raw.githubusercontent.com/BAndysc/WoWDatabaseEditor/master/Examples/darktheme.png)
![screenshot2](https://raw.githubusercontent.com/BAndysc/WoWDatabaseEditor/master/Examples/blueprints_screenshot.png)
![screenshot](https://github.com/BAndysc/WoWDatabaseEditor/blob/d012bc3ffcac3b12328033b29ebf0d8b49df34eb/Examples/screenshot.png?raw=true)
