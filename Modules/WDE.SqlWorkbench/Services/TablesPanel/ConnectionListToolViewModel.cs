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
using WDE.SqlWorkbench.Services.TableUtils;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Services.TablesPanel;

internal partial class ConnectionListToolViewModel : ObservableBase, ITablesToolGroup
{
    public IMessageBoxService MessageBoxService { get; }
    public IMySqlConnector SqlConnector { get; }
    private readonly Lazy<IExtendedSqlEditorService> sqlEditorService;
    [Notify] private bool isLoading;
    [Notify] private bool listUpdated;
    [Notify] private string searchText = "";
    [Notify] [AlsoNotify(nameof(SelectedDatabase), nameof(SelectedTable), nameof(IsTableSelected))] private INodeType? selected;
    
    public DatabaseConnectionData ConnectionData { get; set; }
    public SchemaViewModel? SelectedDatabase => selected as SchemaViewModel;
    public TableViewModel? SelectedTable => selected as TableViewModel;
    
    public string? SelectedSchemaName => SelectedDatabase?.SchemaName ?? SelectedTable?.Schema.SchemaName;
    
    private ObservableCollectionExtended<SchemaViewModel> roots = new();
    public FlatTreeList<SchemaViewModel, IChildType> FlatItems { get; }
    
    public ImageUri Icon => ConnectionData.Icon ?? ImageUri.Empty;
    public string GroupName => ConnectionData.ConnectionName;
    public RgbColor? CustomColor => ConnectionData.Color is { } col ? new RgbColor(col.R, col.G, col.B) : null;
    public int Priority => 0;
    
    private CancellationTokenSource? loadCancellationTokenSource;

    public DelegateCommand SelectRowsCommand { get; }
    public DelegateCommand InspectTableComment { get; }
    public DelegateCommand CopyNameCommand { get; }
    public AsyncAutoCommand CopySelectAllCommand { get; }
    public AsyncAutoCommand CopyInsertCommand { get; }
    public AsyncAutoCommand CopyUpdateCommand { get; }
    public AsyncAutoCommand CopyDeleteCommand { get; }
    public AsyncAutoCommand CopyCreateCommand { get; }
    public DelegateCommand DumpTableCommand { get; }
    public DelegateCommand DropTableCommand { get; }
    public DelegateCommand TruncateTableCommand { get; }
    public DelegateCommand RefreshDatabaseCommand { get; }
    
    public ConnectionListToolViewModel(IMessageBoxService messageBoxService,
        IMySqlConnector sqlConnector,
        Lazy<ITableUtility> utility,
        IClipboardService clipboardService,
        IQueryGenerator queryGenerator,
        IDatabaseDumpService databaseDumpService,
        IQueryDialogService queryDialogService,
        Lazy<IExtendedSqlEditorService> sqlEditorService)
    {
        MessageBoxService = messageBoxService;
        SqlConnector = sqlConnector;
        this.sqlEditorService = sqlEditorService;
        FlatItems = new FlatTreeList<SchemaViewModel, IChildType>(roots);
        
        SelectRowsCommand = new DelegateCommand(() => utility.Value.OpenSelectRows(ConnectionData, SelectedSchemaName!, SelectedTable!.TableName), () => IsTableSelected);
        InspectTableComment = new DelegateCommand(() => utility.Value.InspectTable(ConnectionData, SelectedSchemaName!, SelectedTable!.TableName), () => IsTableSelected);
        CopyNameCommand = new DelegateCommand(() => clipboardService.SetText(SelectedTable?.TableName ?? SelectedSchemaName!), () => IsAnythingSelected);
        CopySelectAllCommand = new AsyncAutoCommand(async () =>
        {
            clipboardService.SetText(await queryGenerator.GenerateSelectAllAsync(ConnectionData.Credentials.WithSchemaName(SelectedSchemaName!), SelectedTable!.TableName));
        }, () => IsTableSelected);
        CopyInsertCommand = new AsyncAutoCommand(async () =>
        {
            clipboardService.SetText(await queryGenerator.GenerateInsertAsync(ConnectionData.Credentials.WithSchemaName(SelectedSchemaName!), SelectedTable!.TableName));
        }, () => IsTableSelected);
        CopyUpdateCommand = new AsyncAutoCommand(async () =>
        {
            clipboardService.SetText(await queryGenerator.GenerateUpdateAsync(ConnectionData.Credentials.WithSchemaName(SelectedSchemaName!), SelectedTable!.TableName));
        }, () => IsTableSelected);
        CopyDeleteCommand = new AsyncAutoCommand(async () =>
        {
            clipboardService.SetText(await queryGenerator.GenerateDeleteAsync(ConnectionData.Credentials.WithSchemaName(SelectedSchemaName!), SelectedTable!.TableName));
        }, () => IsTableSelected);
        CopyCreateCommand = new AsyncAutoCommand(async () =>
        {
            clipboardService.SetText(await queryGenerator.GenerateCreateAsync(ConnectionData.Credentials.WithSchemaName(SelectedSchemaName!), SelectedTable!.TableName));
        }, () => IsTableSelected);
        DumpTableCommand = new DelegateCommand(() => databaseDumpService.ShowDumpTableWindowAsync(ConnectionData.Credentials.WithSchemaName(SelectedSchemaName!), SelectedTable!.TableName).ListenErrors(),
            () => IsTableSelected);
        DropTableCommand = new DelegateCommand(() =>
        {
            queryDialogService.ShowQueryDialog(queryGenerator.GenerateDropTable(SelectedSchemaName, SelectedTable!.TableName));
        }, () => IsTableSelected);
        TruncateTableCommand = new DelegateCommand(() =>
        {
            queryDialogService.ShowQueryDialog(queryGenerator.GenerateTruncateTable(SelectedSchemaName, SelectedTable!.TableName));
        }, () => IsTableSelected);
        RefreshDatabaseCommand = new DelegateCommand(() =>
        {
            var db = SelectedDatabase ?? SelectedTable?.Schema;
            if (db != null)
            {
                db.IsExpanded = false;
                db.IsExpanded = true;
            }
        }, () => IsAnythingSelected);
        
        On(() => SearchText, DoFilter);
        On(() => Selected, _ => RaiseCommandsCanExecuteChanged());
    }

    private void RaiseCommandsCanExecuteChanged()
    {
        SelectRowsCommand.RaiseCanExecuteChanged();
        InspectTableComment.RaiseCanExecuteChanged();
        CopyNameCommand.RaiseCanExecuteChanged();
        CopySelectAllCommand.RaiseCanExecuteChanged();
        CopyInsertCommand.RaiseCanExecuteChanged();
        CopyUpdateCommand.RaiseCanExecuteChanged();
        CopyDeleteCommand.RaiseCanExecuteChanged();
        CopyCreateCommand.RaiseCanExecuteChanged();
        DumpTableCommand.RaiseCanExecuteChanged();
        DropTableCommand.RaiseCanExecuteChanged();
        TruncateTableCommand.RaiseCanExecuteChanged();
        RefreshDatabaseCommand.RaiseCanExecuteChanged();
    }

    public bool IsTableSelected => SelectedTable != null;

    public bool IsDatabaseSelected => SelectedDatabase != null;

    public bool IsAnythingSelected => SelectedTable != null || SelectedDatabase != null;

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
            finally
            {
                IsLoading = false;
            }
        }

        loadCancellationTokenSource?.Cancel();
        loadCancellationTokenSource = new CancellationTokenSource();
        LoadCoreAsync(loadCancellationTokenSource.Token).ListenErrors(MessageBoxService);    
    }

    public void ToolClosed()
    {
        loadCancellationTokenSource?.Cancel();
        loadCancellationTokenSource = null;
    }

    private void DoFilter(string search)
    {
        bool UpdateVisibilityRecursive(SchemaViewModel item)
        {
            bool anyChildVisible = false;
            var matched = string.IsNullOrEmpty(search) || item.SchemaName.Contains(search, StringComparison.OrdinalIgnoreCase);
            item.IsVisible = matched;
            foreach (var child in item.Children)
            {
                var childMatched = string.IsNullOrEmpty(search);
                if (child is TableViewModel tableItem)   
                     childMatched |= tableItem.TableName.Contains(search, StringComparison.OrdinalIgnoreCase);
                child.IsVisible = childMatched;
                anyChildVisible |= childMatched;
            }
            
            // foreach (var child in item.NestedParents.Cast<RawDatabaseItem>())
            //     anyChildVisible |= UpdateVisibilityRecursive(child);
            
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
            sqlEditorService.Value.NewDocumentWithQueryAndExecute(ConnectionData.WithSchemaName(databaseItem.SchemaName),
                $@"SELECT * FROM `information_schema`.`TABLES` WHERE `table_schema` = '{databaseItem.SchemaName}';");
        }
        else if (node is TableViewModel tableItem)
        {
            sqlEditorService.Value.NewDocumentWithTableSelect(ConnectionData.WithSchemaName(tableItem.Schema.SchemaName), tableItem.TableName);
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
        await using var session = await SqlConnector.ConnectAsync(ConnectionData.Credentials);
        var databases = await session.GetDatabasesAsync(token);

        if (ConnectionData.VisibleSchemas != null)
            databases = databases.Intersect(ConnectionData.VisibleSchemas).ToList();
        
        var databasesViewModel = databases
            .Select(x => new SchemaViewModel(this, ConnectionData.Credentials, x))
            .ToList();

        if (ConnectionData.DefaultExpandSchemas)
        {
            databasesViewModel.ForEach(x => x.IsExpanded = true);
        }
        
        return databasesViewModel;
    }
}