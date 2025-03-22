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
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Common.Utils.DragDrop;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.Downloaders.MariaDb;
using WDE.SqlWorkbench.Services.Downloaders.MySql;

namespace WDE.SqlWorkbench.Settings;

[AutoRegister]
[SingleInstance]
internal partial class SqlWorkbenchConfigurationViewModel : ObservableBase, IConfigurable, IDropTarget
{
    private readonly ISqlWorkbenchPreferences preferences;
    private readonly IConnectionsManager connectionsManager;
    private readonly IMariaDownloadService mariaDownloadService;
    [Notify] [AlsoNotify(nameof(IsModified))] private bool connectionsContainerIsModified;
    
    public ICommand Save { get; }
    public ImageUri Icon { get; } = new ImageUri("Icons/document_sql0_big.png");
    public string Name => "SQL Editor";
    public string? ShortDescription => "You can open raw SQL editor and execute queries on your database. Your world/hotfix databases are available by default. If you want to open sessions to other databases, you can provide the credentials here.";
    public bool IsRestartRequired => true;
    public ConfigurableGroup Group => ConfigurableGroup.Advanced;
    public bool IsModified => Connections.Any(x => x.IsModified) ||
                              ConnectionsContainerIsModified || 
                              (DefaultConnection?.Id ?? Guid.Empty) != originalDefaultConnection ||
                              originalUseCodeCompletion != UseCodeCompletion ||
                              originalCustomSqlsPath != CustomSqlsPath ||
                              originalEachDatabaseHasSeparateConnection != EachDatabaseHasSeparateConnection ||
                              originalCustomMySqlDumpPath != CustomMySqlDumpPath ||
                              originalCustomMariaDumpPath != CustomMariaDumpPath ||
                              originalAskBeforeApplyingChanges != AskBeforeApplyingChanges;

    private Guid originalDefaultConnection;
    private bool originalUseCodeCompletion;
    private bool originalAskBeforeApplyingChanges;
    private string? originalCustomSqlsPath;
    private string? originalCustomMariaDumpPath;
    private string? originalCustomMySqlDumpPath;
    private bool originalEachDatabaseHasSeparateConnection;
    private bool originalCloseNonModifiedTabsOnExecute;
    
    public ObservableCollection<ConnectionConfigViewModel> Connections { get; } = new();

    [Notify] private bool useCodeCompletion;
    
    [Notify] private bool eachDatabaseHasSeparateConnection;

    [Notify] private bool closeNonModifiedTabsOnExecute;

    [Notify] private bool askBeforeApplyingChanges;

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
    
    [Notify] [AlsoNotify(nameof(HasCustomMariaDumpPath))] private string? customMariaDumpPath;

    public bool HasCustomMariaDumpPath
    {
        get => customMariaDumpPath != null;
        set
        {
            if (value)
                CustomMariaDumpPath = "";
            else
                CustomMariaDumpPath = null;
        }
    }
    
    [Notify] [AlsoNotify(nameof(HasCustomMySqlDumpPath))] private string? customMySqlDumpPath;

    public bool HasCustomMySqlDumpPath
    {
        get => CustomMySqlDumpPath != null;
        set
        {
            if (value)
                CustomMySqlDumpPath = "";
            else
                CustomMySqlDumpPath = null;
        }
    }

    [Notify] [AlsoNotify(nameof(HasCustomMariaImportPath))] private string? customMariaImportPath;

    public bool HasCustomMariaImportPath
    {
        get => CustomMariaImportPath != null;
        set
        {
            if (value)
                CustomMariaImportPath = "";
            else
                CustomMariaImportPath = null;
        }
    }

    [Notify] [AlsoNotify(nameof(HasCustomMySqlImportPath))] private string? customMySqlImportPath;

    public bool HasCustomMySqlImportPath
    {
        get => CustomMySqlImportPath != null;
        set
        {
            if (value)
                CustomMySqlImportPath = "";
            else
                CustomMySqlImportPath = null;
        }
    }

    [Notify] private ConnectionConfigViewModel? selectedConnection;

    [Notify] [AlsoNotify(nameof(IsModified))] private ConnectionConfigViewModel? defaultConnection;
    
    public IAsyncCommand<ConnectionConfigViewModel> PickDatabaseCommand { get; }
    
    public IAsyncCommand<ConnectionConfigViewModel> PickVisibleDatabasesCommand { get; }
    
    public IAsyncCommand<ConnectionConfigViewModel> PickIconCommand { get; }
    
    public IAsyncCommand PickCustomSqlsPath { get; }
    
    public IAsyncCommand PickCustomMariaDumpPath { get; }

    public IAsyncCommand PickCustomMariaImportPath { get; }

    public IAsyncCommand DownloadAndPickCustomMariaDumpCommand { get; }

    public IAsyncCommand PickCustomMySqlDumpPath { get; }

    public IAsyncCommand PickCustomMySqlImportPath { get; }

    public IAsyncCommand DownloadAndPickCustomMySqlDumpCommand { get; }

    public DelegateCommand AddConnectionCommand { get; }
    
    public DelegateCommand DeleteSelectedConnectionCommand { get; }
    
    public DelegateCommand DuplicateSelectedConnectionCommand { get; }
    
    public SqlWorkbenchConfigurationViewModel(ISqlWorkbenchPreferences preferences,
        IConnectionsManager connectionsManager, // keep it here, as the constructor provides the default connections
        IItemFromListProvider itemFromListProvider,
        IWindowManager windowManager,
        IMySqlConnector mySqlConnector,
        IMessageBoxService messageBoxService,
        IMariaDownloadService mariaDownloadService,
        IMySqlDownloadService mySqlDownloadService,
        IWorldDatabaseSettingsProvider worldDatabaseSettingsProvider,
        IHotfixDatabaseSettingsProvider hotfixDatabaseSettingsProvider,
        IAuthDatabaseSettingsProvider authDatabaseSettingsProvider)
    {
        this.preferences = preferences;
        this.connectionsManager = connectionsManager;
        this.mariaDownloadService = mariaDownloadService;
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
            preferences.CustomMariaDumpPath = CustomMariaDumpPath;
            preferences.CustomMySqlDumpPath = CustomMySqlDumpPath;
            preferences.CustomMariaImportPath = CustomMariaImportPath;
            preferences.CustomMySqlImportPath = CustomMySqlImportPath;
            preferences.EachDatabaseHasSeparateConnection = EachDatabaseHasSeparateConnection;
            preferences.AskBeforeApplyingChanges = AskBeforeApplyingChanges;
            preferences.CloseNonModifiedTabsOnExecute = CloseNonModifiedTabsOnExecute;
            preferences.Save();
            
            originalDefaultConnection = preferences.DefaultConnection ?? Guid.Empty;
            originalUseCodeCompletion = preferences.UseCodeCompletion;
            originalAskBeforeApplyingChanges = preferences.AskBeforeApplyingChanges;
            originalCustomSqlsPath = preferences.CustomSqlsPath;
            originalCustomMariaDumpPath = preferences.CustomMariaDumpPath;
            originalCustomMySqlDumpPath = preferences.CustomMySqlDumpPath;
            originalCloseNonModifiedTabsOnExecute = preferences.CloseNonModifiedTabsOnExecute;
            originalEachDatabaseHasSeparateConnection = preferences.EachDatabaseHasSeparateConnection;
            foreach (var connection in Connections)
                connection.Original = connection.ToConnectionData();
            ConnectionsContainerIsModified = false;
        });

        DatabaseCredentials GetActualCredentials(ConnectionConfigViewModel vm)
        {
            if (vm.CredentialsSource == CredentialsSource.Custom)
                return new DatabaseCredentials(vm.User, vm.Password, vm.Host, vm.Port, vm.DefaultDatabase);
            if (vm.CredentialsSource == CredentialsSource.WorldDatabase)
                return new DatabaseCredentials(worldDatabaseSettingsProvider.Settings);
            if (vm.CredentialsSource == CredentialsSource.AuthDatabase)
                return new DatabaseCredentials(authDatabaseSettingsProvider.Settings);
            if (vm.CredentialsSource == CredentialsSource.HotfixDatabase)
                return new DatabaseCredentials(hotfixDatabaseSettingsProvider.Settings);
            return default;
        }
        
        PickDatabaseCommand = new AsyncAutoCommand<ConnectionConfigViewModel>(async vm =>
        {
            await using var session = await mySqlConnector.ConnectAsync(GetActualCredentials(vm), QueryExecutionSafety.AskUnlessSelect);
            var databases = await session.GetDatabasesAsync();
            var selection = await itemFromListProvider.GetItemFromList(databases.ToDictionary(x => x, x => new SelectOption("")), false);
            if (selection != null)
                vm.DefaultDatabase = selection;
        }).WrapMessageBox<MySqlException, ConnectionConfigViewModel>(messageBoxService);

        PickVisibleDatabasesCommand = new AsyncAutoCommand<ConnectionConfigViewModel>(async vm =>
        {
            await using var session = await mySqlConnector.ConnectAsync(GetActualCredentials(vm), QueryExecutionSafety.AskUnlessSelect);
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
        
        PickCustomMariaDumpPath = new AsyncAutoCommand(async () =>
        {
            var path = await windowManager.ShowOpenFileDialog(CustomMariaDumpPath ?? Environment.CurrentDirectory);
            if (path != null)
                CustomMariaDumpPath = path;
        });

        PickCustomMariaImportPath = new AsyncAutoCommand(async () =>
        {
            var path = await windowManager.ShowOpenFileDialog(CustomMariaImportPath ?? Environment.CurrentDirectory);
            if (path != null)
                CustomMariaImportPath = path;
        });

        DownloadAndPickCustomMariaDumpCommand = new AsyncAutoCommand(async () =>
        {
            var paths = await mariaDownloadService.AskToDownloadMariaAsync();
            if (paths != null)
            {
                CustomMariaDumpPath = paths.Value.dump;
                CustomMariaImportPath = paths.Value.mariadb;
            }
        });

        PickCustomMySqlDumpPath = new AsyncAutoCommand(async () =>
        {
            var path = await windowManager.ShowOpenFileDialog(CustomMySqlDumpPath ?? Environment.CurrentDirectory);
            if (path != null)
                CustomMySqlDumpPath = path;
        });

        PickCustomMySqlImportPath = new AsyncAutoCommand(async () =>
        {
            var path = await windowManager.ShowOpenFileDialog(CustomMySqlImportPath ?? Environment.CurrentDirectory);
            if (path != null)
                CustomMySqlImportPath = path;
        });

        DownloadAndPickCustomMySqlDumpCommand = new AsyncAutoCommand(async () =>
        {
            var paths = await mySqlDownloadService.AskToDownloadMySqlAsync();
            if (paths != null)
            {
                CustomMySqlDumpPath = paths.Value.dump;
                CustomMySqlImportPath = paths.Value.mysql;
            }
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
                null,
                QueryExecutionSafety.AskUnlessSelect));
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
        originalCloseNonModifiedTabsOnExecute = CloseNonModifiedTabsOnExecute = preferences.CloseNonModifiedTabsOnExecute;
        UseCodeCompletion = originalUseCodeCompletion = preferences.UseCodeCompletion;
        AskBeforeApplyingChanges = originalAskBeforeApplyingChanges = preferences.AskBeforeApplyingChanges;
        CustomSqlsPath = originalCustomSqlsPath = preferences.CustomSqlsPath;
        CustomMariaDumpPath = originalCustomMariaDumpPath = preferences.CustomMariaDumpPath;
        CustomMySqlDumpPath = originalCustomMySqlDumpPath = preferences.CustomMySqlDumpPath;
        CustomMariaImportPath = preferences.CustomMariaImportPath;
        CustomMySqlImportPath = preferences.CustomMySqlImportPath;
        ConnectionsContainerIsModified = false;
    }

    public void DragOver(IDropInfo dropInfo)
    {
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        dropInfo.Effects = DragDropEffects.Move;
    }

    public void Drop(IDropInfo dropInfo)
    {
        IReadOnlyList<ConnectionConfigViewModel> dragged;

        if (dropInfo.Data is IReadOnlyList<ConnectionConfigViewModel> dragged2)
        {
            dragged = dragged2;
        }
        else if (dropInfo.Data is ConnectionConfigViewModel drag)
        {
            dragged = new[] { drag };
        }
        else
            return;

        int dropIndex = dropInfo.InsertIndex;

        foreach (var x in dragged)
        {
            int indexOf = Connections.IndexOf(x);
            if (indexOf < dropIndex)
                dropIndex--;
        }

        foreach (var x in dragged)
        {
            Connections.Remove(x);
        }
        
        foreach (var x in dragged)
        {
            Connections.Insert(dropIndex++, x);
            SelectedConnection = x;
        }
    }
}