using System;
using Newtonsoft.Json;
using WDE.Common.Parameters;

namespace WDE.Common.Services.FindAnywhere;

[Flags]
public enum FindAnywhereSourceType
{
    None = 0,
    SmartScripts = 1,
    Tables = 2,
    Spawns = 4,
    Conditions = 8,
    Sniffs = 16,
    Dbc = 32,
    EventAi = 64,
    Other = 128,
    SourceCode = 256,
    Waypoints = 512,
    All = Other | SmartScripts | EventAi | Spawns | Tables | Conditions | Sniffs | Dbc | SourceCode | Waypoints
}