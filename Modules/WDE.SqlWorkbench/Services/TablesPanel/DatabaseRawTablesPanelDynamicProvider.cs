using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Ioc;
using WDE.Common.Documents;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Services.TablesPanel;

[AutoRegister]
[SingleInstance]
internal class ConnectionsTablesToolProvider : ITablesToolGroupsProvider
{
    private readonly IConnectionsManager connectionsManager;
    private readonly IContainerProvider containerProvider;

    public ConnectionsTablesToolProvider(IConnectionsManager connectionsManager,
        IContainerProvider containerProvider)
    {
        this.connectionsManager = connectionsManager;
        this.containerProvider = containerProvider;
    }
    
    public IEnumerable<ITablesToolGroup> GetProviders()
    {
        return connectionsManager.StaticConnections.Select(x =>
        {
            var vm = containerProvider.Resolve<ConnectionListToolViewModel>((typeof(IConnection), x));
            return vm;
        });
    }
}