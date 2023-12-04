using System.Collections.Generic;
using System.Collections.ObjectModel;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.Connection;

[AutoRegister]
[SingleInstance]
internal interface IConnectionsManager
{
    /// <summary>
    /// all connections, including predefined in the settings and dynamically created
    /// </summary>
    ObservableCollection<IConnection> AllConnections { get; }
    /// <summary>
    /// all connections which are predefined in the settings
    /// </summary>
    IReadOnlyList<IConnection> StaticConnections { get; set; }
    IConnection? DefaultConnection { get; set; }
    IConnection Clone(IConnection baseConnection, string schemaName);
}