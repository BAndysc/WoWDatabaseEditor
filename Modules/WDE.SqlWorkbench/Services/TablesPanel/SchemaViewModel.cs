using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Binding;
using PropertyChanged.SourceGenerator;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Services.TablesPanel;
/*
 * Hierarchy:
 *
 * - SchemaViewModel
 *   |
 *   | - DatabaseObjectGroupViewModel (Tables)
 *        | - TableViewModel
 *   | - DatabaseObjectGroupViewModel (Views)
 *        | - TableViewModel
 *   | - DatabaseObjectGroupViewModel (Procedures)
 *        | - RoutineViewModel
 *   | - DatabaseObjectGroupViewModel (Functions)
 *       | - RoutineViewModel
 */

internal partial class SchemaViewModel : ObservableBase, INamedParentType
{
    private readonly IConnection connection;
    [Notify] private bool isExpanded;
    
    public ConnectionListToolViewModel ParentViewModel { get; }
    public ImageUri Icon => new ImageUri("Icons/icon_mini_schema_big.png");
    public string SchemaName { get; }
    public string Name => SchemaName;
    
    private ObservableCollectionExtended<INamedChildType> children = new ObservableCollectionExtended<INamedChildType>();
    private ObservableCollectionExtended<DatabaseObjectGroupViewModel> groups = new ObservableCollectionExtended<DatabaseObjectGroupViewModel>();

    private CancellationTokenSource? loadToken;
    
    public IConnection Connection => connection;
    
    public SchemaViewModel(ConnectionListToolViewModel parentViewModel,
        IConnection connection,
        string schemaName)
    {
        this.connection = connection;
        ParentViewModel = parentViewModel;
        SchemaName = schemaName;
        
        groups.CollectionChanged += (sender, args) => NestedParentsChanged?.Invoke(this, args);
        children.CollectionChanged += (sender, args) => ChildrenChanged?.Invoke(this, args);
        On(() => IsExpanded, @is =>
        {
            loadToken?.Cancel();
            if (@is)
            {
                loadToken = new CancellationTokenSource();
                LoadTablesAsync(loadToken.Token).ListenErrors();
            }
            else
                loadToken = null;
        });
    }
    
    private async Task LoadTablesAsync(CancellationToken token)
    {
        children.RemoveAll();
        groups.RemoveAll();
        children.Add(new RowLoadingViewModel(this));

        IReadOnlyList<TableInfo> tablesAndViews = Array.Empty<TableInfo>();
        IReadOnlyList<RoutineInfo> routines = Array.Empty<RoutineInfo>();
        try
        {
            await using var session = await connection.OpenSessionAsync();

            if (token.IsCancellationRequested)
                return;

            tablesAndViews = await session.GetTablesAsync(SchemaName, token);

            if (token.IsCancellationRequested)
                return;

            routines = await session.GetRoutinesAsync(SchemaName, token);

            if (token.IsCancellationRequested)
                return;
        }
        catch (Exception e)
        {
            await ParentViewModel.UserQuestionsService.ConnectionsErrorAsync(e);
            throw;
        }
        finally
        {
            children.RemoveAll();
        }

        var tables = tablesAndViews
            .Where(x => x.Type == TableType.Table)
            .Select(x => new TableViewModel(this, x.Name, TableType.Table))
            .ToList();
        var views = tablesAndViews
            .Where(x => x.Type != TableType.Table)
            .Select(x => new TableViewModel(this, x.Name, TableType.View))
            .ToList();
        var procedures = routines
            .Where(x => x.Type == RoutineType.Procedure)
            .Select(x => new RoutineViewModel(this, x.Name, RoutineType.Procedure))
            .ToList();
        var functions = routines
            .Where(x => x.Type == RoutineType.Function)
            .Select(x => new RoutineViewModel(this, x.Name, RoutineType.Function))
            .ToList();
        
        groups.RemoveAll();
        groups.Add(new DatabaseObjectGroupViewModel(this, "Functions", new ImageUri("Icons/icon_mini_func.png"), functions));
        groups.Add(new DatabaseObjectGroupViewModel(this, "Procedures", new ImageUri("Icons/icon_mini_proc.png"), procedures));
        groups.Add(new DatabaseObjectGroupViewModel(this, "Views", new ImageUri("Icons/icon_mini_view.png"), views));
        groups.Add(new DatabaseObjectGroupViewModel(this, "Tables", new ImageUri("Icons/icon_mini_table.png"), tables));
        
        groups.Each(x => x.IsExpanded = true);
        
        ParentViewModel.Refilter();
    }

    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
    public IReadOnlyList<IParentType> NestedParents => groups;
    public IReadOnlyList<IChildType> Children => children;
    public bool CanBeExpanded => true;

    public event NotifyCollectionChangedEventHandler? ChildrenChanged;
    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;
}