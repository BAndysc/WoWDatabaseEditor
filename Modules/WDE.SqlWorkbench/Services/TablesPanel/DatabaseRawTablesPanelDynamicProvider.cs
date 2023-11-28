using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Ioc;
using WDE.Common.Documents;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Services.TablesPanel;

[AutoRegister]
[SingleInstance]
internal class ConnectionsTablesToolProvider : ITablesToolGroupsProvider
{
    private readonly IConnectionsManager connectionsManager;
    private readonly Func<ConnectionListToolViewModel> factory;

    public ConnectionsTablesToolProvider(IConnectionsManager connectionsManager,
        Func<ConnectionListToolViewModel> factory)
    {
        this.connectionsManager = connectionsManager;
        this.factory = factory;
    }
    
    public IEnumerable<ITablesToolGroup> GetProviders()
    {
        return connectionsManager.Connections.Select(x =>
        {
            var vm = factory();
            vm.ConnectionData = x;
            return vm;
        });
    }
}