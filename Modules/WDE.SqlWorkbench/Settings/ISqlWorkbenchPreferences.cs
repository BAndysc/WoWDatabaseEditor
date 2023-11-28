using System;
using System.Collections.Generic;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Settings;

[UniqueProvider]
internal interface ISqlWorkbenchPreferences
{
    IReadOnlyList<DatabaseConnectionData> Connections { get; set; }
    Guid? DefaultConnection { get; set; }
    bool UseCodeCompletion { get; set; }
    string? CustomSqlsPath { get; set; }
    void Save();
}