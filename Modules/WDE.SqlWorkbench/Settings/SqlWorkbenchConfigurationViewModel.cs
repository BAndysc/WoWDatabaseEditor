using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using MySqlConnector;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Settings;

[AutoRegister]
[SingleInstance]
internal partial class SqlWorkbenchConfigurationViewModel : ObservableBase, IConfigurable
{
    private readonly ISqlWorkbenchPreferences preferences;
    private readonly IConnectionsManager connectionsManager;
    [Notify] [AlsoNotify(nameof(IsModified))] private bool connectionsContainerIsModified;
    
    public ICommand Save { get; }
    public string Name => "SQL Editor";
    public string? ShortDescription => "You can open raw SQL editor and execute queries on your database. Your world/hotfix databases are available by default. If you want to open sessions to other databases, you can provide the credentials here.";
    public bool IsRestartRequired => true;
    public ConfigurableGroup Group => ConfigurableGroup.Advanced;
    public bool IsModified => Connections.Any(x => x.IsModified) ||
                              ConnectionsContainerIsModified || 
                              (DefaultConnection?.Id ?? Guid.Empty) != originalDefaultConnection ||
                              originalUseCodeCompletion != UseCodeCompletion ||
                              originalCustomSqlsPath != CustomSqlsPath ||
                              originalEachDatabaseHasSeparateConnection != EachDatabaseHasSeparateConnection;

    private Guid originalDefaultConnection;
    private bool originalUseCodeCompletion;
    private string? originalCustomSqlsPath;
    private bool originalEachDatabaseHasSeparateConnection;
    
    public ObservableCollection<ConnectionConfigViewModel> Connections { get; } = new();

    [Notify] private bool useCodeCompletion;
    
    [Notify] private bool eachDatabaseHasSeparateConnection;

    [Notify] [AlsoNotify(nameof(HasCustomSqlsPath))] private string? customSqlsPath;

    public bool HasCustomSqlsPath
    {
        get => customSqlsPath != null;
        set
        {
            if (value)
                CustomSqlsPath = "";
            else
                CustomSqlsPath = null;
        }
    }
    
    [Notify] private ConnectionConfigViewModel? selectedConnection;

    [Notify] [AlsoNotify(nameof(IsModified))] private ConnectionConfigViewModel? defaultConnection;
    
    public IAsyncCommand<ConnectionConfigViewModel> PickDatabaseCommand { get; }
    
    public IAsyncCommand<ConnectionConfigViewModel> PickVisibleDatabasesCommand { get; }
    
    public IAsyncCommand<ConnectionConfigViewModel> PickIconCommand { get; }
    
    public IAsyncCommand PickCustomSqlsPath { get; }
    
    public DelegateCommand AddConnectionCommand { get; }
    
    public DelegateCommand DeleteSelectedConnectionCommand { get; }
    
    public DelegateCommand DuplicateSelectedConnectionCommand { get; }
    
    public SqlWorkbenchConfigurationViewModel(ISqlWorkbenchPreferences preferences,
        IConnectionsManager connectionsManager, // keep it here, as the constructor provides the default connections
        IItemFromListProvider itemFromListProvider,
        IWindowManager windowManager,
        IMySqlConnector mySqlConnector,
        IMessageBoxService messageBoxService)
    {
        this.preferences = preferences;
        this.connectionsManager = connectionsManager;
        Connections.ToStream(true).SubscribeAction(e =>
        {
            if (e.Type == CollectionEventType.Add)
                e.Item.PropertyChanged += ItemOnPropertyChanged;
            else if (e.Type == CollectionEventType.Remove)
                e.Item.PropertyChanged -= ItemOnPropertyChanged;
        });
        Connections.CollectionChanged += ConnectionsOnCollectionChanged;
        
        Save = new DelegateCommand(() =>
        {
            preferences.Connections = Connections.Select(x => x.ToConnectionData()).ToList();
            preferences.DefaultConnection = DefaultConnection?.Id;
            preferences.UseCodeCompletion = UseCodeCompletion;
            preferences.CustomSqlsPath = CustomSqlsPath;
            preferences.EachDatabaseHasSeparateConnection = EachDatabaseHasSeparateConnection;
            preferences.Save();
            
            originalDefaultConnection = preferences.DefaultConnection ?? Guid.Empty;
            originalUseCodeCompletion = preferences.UseCodeCompletion;
            originalCustomSqlsPath = preferences.CustomSqlsPath;
            originalEachDatabaseHasSeparateConnection = preferences.EachDatabaseHasSeparateConnection;
            foreach (var connection in Connections)
                connection.Original = connection.ToConnectionData();
            ConnectionsContainerIsModified = false;
        });
        
        PickDatabaseCommand = new AsyncAutoCommand<ConnectionConfigViewModel>(async vm =>
        {
            await using var session = await mySqlConnector.ConnectAsync(new DatabaseCredentials(vm.User, vm.Password, vm.Host, vm.Port, vm.DefaultDatabase));
            var databases = await session.GetDatabasesAsync();
            var selection = await itemFromListProvider.GetItemFromList(databases.ToDictionary(x => x, x => new SelectOption("")), false);
            if (selection != null)
                vm.DefaultDatabase = selection;
        }).WrapMessageBox<MySqlException, ConnectionConfigViewModel>(messageBoxService);

        PickVisibleDatabasesCommand = new AsyncAutoCommand<ConnectionConfigViewModel>(async vm =>
        {
            await using var session = await mySqlConnector.ConnectAsync(new DatabaseCredentials(vm.User, vm.Password, vm.Host, vm.Port, vm.DefaultDatabase));
            var databases = await session.GetDatabasesAsync();
            var selection = await itemFromListProvider.GetItemFromList(databases.ToDictionary(x => x, x => new SelectOption("")), true, string.Join(' ', (IReadOnlyList<string>?)vm.VisibleSchemas ?? Array.Empty<string>()));
            if (selection != null)
                vm.VisibleSchemas = selection.Split(' ').ToList();
        });
        
        PickIconCommand = new AsyncAutoCommand<ConnectionConfigViewModel>(async vm =>
        {
            var file = new FileInfo(vm.Icon ?? Environment.CurrentDirectory);
            var icon = await windowManager.ShowOpenFileDialog("Images|png,jpg,jpeg,bmp,gif", file.Exists ? file.Directory?.FullName : null);
            if (icon != null)
                vm.Icon = icon;
        });

        PickCustomSqlsPath = new AsyncAutoCommand(async () =>
        {
            var path = await windowManager.ShowOpenFileDialog(customSqlsPath ?? Environment.CurrentDirectory);
            if (path != null)
                CustomSqlsPath = path;
        });
        
        AddConnectionCommand = new DelegateCommand(() =>
        {
            var newConnection = new ConnectionConfigViewModel(new DatabaseConnectionData(Guid.NewGuid(), 
                CredentialsSource.Custom,
                new DatabaseCredentials("", "", "localhost", 3306, ""), 
                "New connection", 
                null,
                false,
                null,
                null));
            Connections.Add(newConnection);
            SelectedConnection = newConnection;
        });
        DeleteSelectedConnectionCommand = new DelegateCommand(() =>
        {
            if (SelectedConnection != null)
            {
                Connections.Remove(SelectedConnection);
                SelectedConnection = null;
            }
        }, () => SelectedConnection != null).ObservesProperty(() => SelectedConnection);
        DuplicateSelectedConnectionCommand = new DelegateCommand(() =>
        {
            if (SelectedConnection != null)
            {
                var newConnection = new ConnectionConfigViewModel(SelectedConnection.ToConnectionData().WithId(Guid.NewGuid()));
                Connections.Add(newConnection);
                SelectedConnection = newConnection;
            }
        }, () => SelectedConnection != null).ObservesProperty(() => SelectedConnection);
    }

    private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConnectionConfigViewModel.IsModified))
            RaisePropertyChanged(nameof(IsModified));
    }

    private void ConnectionsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ConnectionsContainerIsModified = true;
    }

    public void ConfigurableOpened()
    {
        Connections.RemoveAll();
        foreach (var connection in preferences.Connections)
            Connections.Add(new ConnectionConfigViewModel(connection));
        DefaultConnection = Connections.FirstOrDefault(c => c.Id == preferences.DefaultConnection);
        SelectedConnection = DefaultConnection ?? Connections.FirstOrDefault();
        originalDefaultConnection = preferences.DefaultConnection ?? Guid.Empty;
        originalEachDatabaseHasSeparateConnection = EachDatabaseHasSeparateConnection = preferences.EachDatabaseHasSeparateConnection;
        UseCodeCompletion = originalUseCodeCompletion = preferences.UseCodeCompletion;
        CustomSqlsPath = originalCustomSqlsPath = preferences.CustomSqlsPath;
        ConnectionsContainerIsModified = false;
    }
}