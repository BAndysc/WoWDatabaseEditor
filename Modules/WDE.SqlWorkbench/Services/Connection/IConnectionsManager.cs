using System.Collections.Generic;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.Connection;

[AutoRegister]
[SingleInstance]
internal interface IConnectionsManager
{
    IReadOnlyList<DatabaseConnectionData> Connections { get; set; }
    DatabaseConnectionData? DefaultConnection { get; set; }
}