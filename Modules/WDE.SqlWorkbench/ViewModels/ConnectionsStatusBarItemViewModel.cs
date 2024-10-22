using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.ViewModels;

[AutoRegister]
[SingleInstance]
internal partial class ConnectionsStatusBarItemViewModel : ObservableBase, IConnectionsStatusBarItem
{
    private readonly IConnectionsManager connectionsManager;

    public ConnectionsStatusBarItemViewModel(IConnectionsManager connectionsManager)
    {
        this.connectionsManager = connectionsManager;
        AutoDispose(connectionsManager.AllConnections
            .ToStream(true)
            .SubscribeAction(x =>
            {
                if (x.Type == CollectionEventType.Add)
                    x.Item.PropertyChanged += OnConnectionPropertyChanged;
                else if (x.Type == CollectionEventType.Remove)
                    x.Item.PropertyChanged -= OnConnectionPropertyChanged;
            }));
    }

    private void OnConnectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IConnection.IsOpened))
        {
            RaisePropertyChanged(nameof(OpenedConnections));
            if (sender is IConnection connection)
            {
                if (connection.IsOpened)
                    OpenedConnectionsList.Add(connection);
                else
                    OpenedConnectionsList.Remove(connection);
            }
        }
    }

    public int OpenedConnections => connectionsManager.AllConnections.Count(x => x.IsOpened);

    public ObservableCollection<IConnection> OpenedConnectionsList { get; } = new();
}