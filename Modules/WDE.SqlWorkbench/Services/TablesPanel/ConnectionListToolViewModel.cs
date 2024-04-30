using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData.Binding;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Documents;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.SqlDump;
using WDE.SqlWorkbench.Services.SqlImport;
using WDE.SqlWorkbench.Services.TableUtils;
using WDE.SqlWorkbench.Services.UserQuestions;
using WDE.SqlWorkbench.Settings;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Services.TablesPanel;

internal interface INamedNodeType : INodeType
{
    string Name { get; }
    ImageUri Icon { get; }
}

internal interface INamedParentType : IParentType, INamedNodeType
{
}

internal interface INamedChildType : IChildType, INamedNodeType
{
}


internal partial class ConnectionListToolViewModel : ObservableBase, ITablesToolGroup
{
    private readonly IConnection connection;
    private readonly IConnectionsManager connectionsManager;
    private readonly Lazy<IExtendedSqlEditorService> sqlEditorService;
    [Notify] private bool isLoading;
    [Notify] private bool listUpdated;
    [Notify] private string searchText = "";
    [Notify] [AlsoNotify(nameof(SelectedDatabase), nameof(SelectedTable), nameof(SelectedRoutine), nameof(SelectedGroup),
        nameof(IsTableSelected), nameof(IsDatabaseSelected), nameof(IsRoutineSelected), nameof(IsTableNotViewSelected), nameof(IsViewSelected))] private INodeType? selected;

    public IUserQuestionsService UserQuestionsService { get; }
    public ISqlWorkbenchPreferences Preferences { get; }
    public SchemaViewModel? SelectedDatabase => selected as SchemaViewModel;
    public TableViewModel? SelectedTable => selected as TableViewModel;
    public RoutineViewModel? SelectedRoutine => selected as RoutineViewModel;
    public DatabaseObjectGroupViewModel? SelectedGroup => selected as DatabaseObjectGroupViewModel;
    
    public SchemaViewModel? SelectedDatabaseOrParent => SelectedDatabase ?? SelectedTable?.Schema ?? SelectedRoutine?.Schema ?? SelectedGroup?.Schema;
    
    public string? SelectedSchemaName => SelectedDatabase?.SchemaName ?? SelectedTable?.Schema.SchemaName ?? SelectedRoutine?.Schema.SchemaName ?? SelectedGroup?.Schema.SchemaName;
    
    public IConnection? SelectedConnection => SelectedDatabase?.Connection 
        ?? SelectedTable?.Schema.Connection
        ?? SelectedRoutine?.Schema.Connection
        ?? SelectedGroup?.Schema.Connection;
    
    private ObservableCollectionExtended<SchemaViewModel> roots = new();
    public FlatTreeList<INamedParentType, INamedChildType> FlatItems { get; }
    
    public ImageUri Icon => connection.ConnectionData.Icon ?? ImageUri.Empty;
    public string GroupName => connection.ConnectionData.ConnectionName;
    public RgbColor? CustomColor => connection.ConnectionData.Color is { } col ? new RgbColor(col.R, col.G, col.B) : null;
    public int Priority => 0;
    
    private CancellationTokenSource? loadCancellationTokenSource;

    public DelegateCommand OpenTabCommand { get; }
    public DelegateCommand SelectRowsCommand { get; }
    public DelegateCommand InspectTableCommand { get; }
    public DelegateCommand InspectDatabaseCommand { get; }
    public DelegateCommand AlterTableComment { get; }
    public DelegateCommand CopyNameCommand { get; }
    public AsyncAutoCommand CopySelectAllCommand { get; }
    public AsyncAutoCommand CopyInsertCommand { get; }
    public AsyncAutoCommand CopyUpdateCommand { get; }
    public AsyncAutoCommand CopyDeleteCommand { get; }
    public AsyncAutoCommand CopyCreateCommand { get; }
    public DelegateCommand ImportDatabaseCommand { get; }
    public DelegateCommand DumpDatabaseCommand { get; }
    public DelegateCommand DumpTableCommand { get; }
    public DelegateCommand DropTableCommand { get; }
    public DelegateCommand TruncateTableCommand { get; }
    public DelegateCommand RefreshDatabaseCommand { get; }
    public DelegateCommand OpenSeparateConnectionCommand { get; }
    public DelegateCommand AddNewTableCommand { get; }
    public DelegateCommand AddNewViewCommand { get; }
    public DelegateCommand AddNewProcedureCommand { get; }
    public DelegateCommand AddNewFunctionCommand { get; }
    
    public ConnectionListToolViewModel(IUserQuestionsService userQuestionsService,
        Lazy<ITableUtility> utility,
        IClipboardService clipboardService,
        IQueryGenerator queryGenerator,
        IDatabaseDumpService databaseDumpService,
        IDatabaseImportService databaseImportService,
        IQueryDialogService queryDialogService,
        IConnection connection,
        IConnectionsManager connectionsManager,
        ISqlWorkbenchPreferences preferences,
        Lazy<IExtendedSqlEditorService> sqlEditorService)
    {
        UserQuestionsService = userQuestionsService;
        Preferences = preferences;
        this.connection = connection;
        this.connectionsManager = connectionsManager;
        this.sqlEditorService = sqlEditorService;
        FlatItems = new FlatTreeList<INamedParentType, INamedChildType>(roots);
        
        OpenTabCommand = new DelegateCommand(() => OpenItem(Selected!), () => IsDatabaseSelected);
        SelectRowsCommand = new DelegateCommand(() => utility.Value.OpenSelectRows(SelectedConnection!, SelectedSchemaName!, SelectedTable!.TableName), () => IsTableSelected);
        AlterTableComment = new DelegateCommand(() => utility.Value.AlterTable(SelectedConnection!, SelectedSchemaName!, SelectedTable!.TableName), () => IsTableSelected);
        InspectTableCommand = new DelegateCommand(() => utility.Value.InspectTable(SelectedConnection!, SelectedSchemaName!, SelectedTable!.TableName), () => IsTableSelected);
        InspectDatabaseCommand = new DelegateCommand(() => utility.Value.InspectDatabase(SelectedConnection!, SelectedSchemaName!), () => IsDatabaseSelected);
        CopyNameCommand = new DelegateCommand(() => clipboardService.SetText(SelectedTable?.TableName ?? SelectedRoutine?.RoutineName ?? SelectedSchemaName!), () => IsAnythingSelected);
        CopySelectAllCommand = new AsyncAutoCommand(async () =>
        {
            await using var session = await SelectedConnection!.OpenSessionAsync();
            clipboardService.SetText(await queryGenerator.GenerateSelectAllAsync(session, SelectedSchemaName!, SelectedTable!.TableName));
        }, () => IsTableSelected);
        CopyInsertCommand = new AsyncAutoCommand(async () =>
        {
            await using var session = await SelectedConnection!.OpenSessionAsync();
            clipboardService.SetText(await queryGenerator.GenerateInsertAsync(session, SelectedSchemaName!, SelectedTable!.TableName));
        }, () => IsTableSelected);
        CopyUpdateCommand = new AsyncAutoCommand(async () =>
        {
            await using var session = await SelectedConnection!.OpenSessionAsync();
            clipboardService.SetText(await queryGenerator.GenerateUpdateAsync(session, SelectedSchemaName!, SelectedTable!.TableName));
        }, () => IsTableSelected);
        CopyDeleteCommand = new AsyncAutoCommand(async () =>
        {
            await using var session = await SelectedConnection!.OpenSessionAsync();
            clipboardService.SetText(await queryGenerator.GenerateDeleteAsync(session, SelectedSchemaName!, SelectedTable!.TableName));
        }, () => IsTableSelected);
        CopyCreateCommand = new AsyncAutoCommand(async () =>
        {
            await using var session = await SelectedConnection!.OpenSessionAsync();
            clipboardService.SetText(await queryGenerator.GenerateCreateAsync(session, SelectedSchemaName!, SelectedTable!.TableName));
        }, () => IsTableSelected);

        AddNewTableCommand = new DelegateCommand(() =>
        {
            utility.Value.AlterTable(SelectedConnection!, SelectedSchemaName!, null);
        }, () => Selected != null);
        
        AddNewViewCommand = new DelegateCommand(() =>
        {
            var tableName = (SelectedTable is { Type: TableType.Table } t) ? t.TableName : "<TABLE_NAME>";
            this.sqlEditorService.Value.NewDocumentWithQuery(SelectedConnection!, $"CREATE VIEW <NAME> AS\nSELECT\n    *\nFROM\n    {tableName};");
        }, () => Selected != null);
        
        AddNewProcedureCommand = new DelegateCommand(() =>
        {
            this.sqlEditorService.Value.NewDocumentWithQuery(SelectedConnection!, "delimiter //\nCREATE PROCEDURE <NAME> (<PARAMS>)\nBEGIN\n    ...\nEND //");
        }, () => Selected != null);
        
        AddNewFunctionCommand = new DelegateCommand(() =>
        {
            this.sqlEditorService.Value.NewDocumentWithQuery(SelectedConnection!, "delimiter //\nCREATE FUNCTION <NAME> (<PARAMS>)\nRETURNS <TYPE>\nBEGIN\n    ...\nEND //");
        }, () => Selected != null);

        ImportDatabaseCommand = new DelegateCommand(() => databaseImportService.ShowImportDatabaseWindowAsync(SelectedConnection!.ConnectionData.Credentials.WithSchemaName(SelectedSchemaName!)).ListenErrors(),
            () => IsDatabaseSelected);
        DumpDatabaseCommand = new DelegateCommand(() => databaseDumpService.ShowDumpDatabaseWindowAsync(SelectedConnection!.ConnectionData.Credentials.WithSchemaName(SelectedSchemaName!)).ListenErrors(),
            () => IsDatabaseSelected);
        DumpTableCommand = new DelegateCommand(() => databaseDumpService.ShowDumpTableWindowAsync(SelectedConnection!.ConnectionData.Credentials.WithSchemaName(SelectedSchemaName!), SelectedTable!.TableName).ListenErrors(),
            () => IsTableSelected);
        DropTableCommand = new DelegateCommand(() =>
        {
            queryDialogService.ShowQueryDialog(queryGenerator.GenerateDropTable(SelectedSchemaName, SelectedTable!.TableName, SelectedTable!.Type));
        }, () => IsTableSelected);
        TruncateTableCommand = new DelegateCommand(() =>
        {
            queryDialogService.ShowQueryDialog(queryGenerator.GenerateTruncateTable(SelectedSchemaName, SelectedTable!.TableName));
        }, () => IsTableSelected);
        RefreshDatabaseCommand = new DelegateCommand(() =>
        {
            var db = SelectedDatabase ?? SelectedTable?.Schema ?? SelectedRoutine?.Schema ?? SelectedGroup?.Schema;
            if (db != null)
            {
                db.IsExpanded = false;
                db.IsExpanded = true;
            }
        }, () => Selected != null);
        OpenSeparateConnectionCommand = new DelegateCommand(() =>
        {
            var newConn = connectionsManager.Clone(SelectedDatabaseOrParent!.Connection, SelectedDatabaseOrParent.SchemaName);
            sqlEditorService.Value.NewDocument(newConn);
        }, () => Selected != null);
        
        On(() => SearchText, DoFilter);
        On(() => Selected, _ => RaiseCommandsCanExecuteChanged());
    }

    private void RaiseCommandsCanExecuteChanged()
    {
        OpenTabCommand.RaiseCanExecuteChanged();
        SelectRowsCommand.RaiseCanExecuteChanged();
        AlterTableComment.RaiseCanExecuteChanged();
        InspectDatabaseCommand.RaiseCanExecuteChanged();
        InspectTableCommand.RaiseCanExecuteChanged();
        CopyNameCommand.RaiseCanExecuteChanged();
        CopySelectAllCommand.RaiseCanExecuteChanged();
        CopyInsertCommand.RaiseCanExecuteChanged();
        CopyUpdateCommand.RaiseCanExecuteChanged();
        CopyDeleteCommand.RaiseCanExecuteChanged();
        CopyCreateCommand.RaiseCanExecuteChanged();
        AddNewTableCommand.RaiseCanExecuteChanged();
        AddNewViewCommand.RaiseCanExecuteChanged();
        AddNewProcedureCommand.RaiseCanExecuteChanged();
        AddNewFunctionCommand.RaiseCanExecuteChanged();
        DumpDatabaseCommand.RaiseCanExecuteChanged();
        DumpTableCommand.RaiseCanExecuteChanged();
        DropTableCommand.RaiseCanExecuteChanged();
        TruncateTableCommand.RaiseCanExecuteChanged();
        RefreshDatabaseCommand.RaiseCanExecuteChanged();
    }

    public bool IsTableNotViewSelected => SelectedTable != null && SelectedTable.Type == TableType.Table;

    public bool IsTableSelected => SelectedTable != null;

    public bool IsViewSelected => SelectedTable != null && SelectedTable.Type is TableType.View;

    public bool IsDatabaseSelected => SelectedDatabase != null;
    
    public bool IsRoutineSelected => SelectedRoutine != null;

    public bool IsAnythingSelected => IsTableSelected || IsDatabaseSelected || IsRoutineSelected;

    public void ToolOpened()
    {
        async Task LoadCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsLoading = true;
                roots.Clear();
                var items = await GetItemsAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return;

                foreach (var item in items)
                    roots.Add(item);

                DoFilter(SearchText);
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                await UserQuestionsService.ConnectionsErrorAsync(e);
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        loadCancellationTokenSource?.Cancel();
        loadCancellationTokenSource = new CancellationTokenSource();
        LoadCoreAsync(loadCancellationTokenSource.Token).ListenErrors();    
    }

    public void ToolClosed()
    {
        loadCancellationTokenSource?.Cancel();
        loadCancellationTokenSource = null;
    }

    private void DoFilter(string search)
    {
        bool UpdateVisibility(DatabaseObjectGroupViewModel item)
        {
            bool anyChildVisible = false;
            foreach (var child in item.Children)
            {
                var childMatched = string.IsNullOrEmpty(search);
                if (child is INamedChildType namedChild)   
                    childMatched |= namedChild.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
                child.IsVisible = childMatched;
                anyChildVisible |= childMatched;
            }
            
            return item.IsVisible = anyChildVisible;
        }
        
        bool UpdateVisibilityRecursive(SchemaViewModel item)
        {
            bool anyChildVisible = false;
            var matched = string.IsNullOrEmpty(search) || item.SchemaName.Contains(search, StringComparison.OrdinalIgnoreCase);
            item.IsVisible = matched;
            
            foreach (var nestedParent in item.NestedParents.Cast<DatabaseObjectGroupViewModel>())
                anyChildVisible |= UpdateVisibility(nestedParent);
            
            if (anyChildVisible)
                item.IsVisible = true;
            
            return anyChildVisible || matched;
        }
        
        foreach (var x in roots)
            UpdateVisibilityRecursive(x);

        ListUpdated = !ListUpdated;
    }

    public void OpenItem(INodeType node)
    {
        if (node is SchemaViewModel databaseItem)
        {
            sqlEditorService.Value.NewDocumentWithQueryAndExecute(databaseItem.Connection, "USE " + databaseItem.SchemaName);
        }
        else if (node is TableViewModel tableItem)
        {
            sqlEditorService.Value.NewDocumentWithTableSelect(tableItem.Schema.Connection, tableItem.Schema.SchemaName, tableItem.TableName);
        }
        else if (node is RoutineViewModel routineItem)
        {
            // todo
        }
        else if (node is DatabaseObjectGroupViewModel group)
        {
            // ignore
        }
        else if (node is RowLoadingViewModel _)
        {
            // ignore
        }
        else
            throw new InvalidOperationException("Unknown node type: " + node.GetType());
    }
    
    public bool OpenSelected()
    {
        if (selected != null)
        {
            OpenItem(selected);
            return true;
        }
        else if (FlatItems.FirstOrDefault(x => x.IsVisible) is { } firstVisibleNode)
        {
            OpenItem(firstVisibleNode);
            return true;
        }
    
        return false;
    }
    
    public void Refilter() => DoFilter(SearchText);
    
    public async Task<IReadOnlyList<SchemaViewModel>> GetItemsAsync(CancellationToken token)
    {
        await using var session = await connection.OpenSessionAsync();
        var databases = await session.GetDatabasesAsync(token);

        if (connection.ConnectionData.VisibleSchemas != null)
            databases = databases.Intersect(connection.ConnectionData.VisibleSchemas).ToList();
        
        var databasesViewModel = databases
            .Select(x => new SchemaViewModel(this, Preferences.EachDatabaseHasSeparateConnection ? connectionsManager.Clone(connection, x) : connection, x))
            .ToList();

        if (connection.ConnectionData.DefaultExpandSchemas)
        {
            databasesViewModel.ForEach(x =>
            {
                x.IsExpanded = true;
            });
        }
        
        return databasesViewModel;
    }
}