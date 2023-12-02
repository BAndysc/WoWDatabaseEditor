using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.Connection;

[AutoRegister]
[SingleInstance]
internal interface IConnectionsManager
{
    IReadOnlyList<IConnection> Connections { get; set; }
    IConnection? DefaultConnection { get; set; }
}