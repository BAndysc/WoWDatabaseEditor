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

namespace WDE.SqlWorkbench.Services.TablesPanel;

internal partial class SchemaViewModel : ObservableBase, IParentType
{
    private readonly DatabaseCredentials credentials;
    [Notify] private bool isExpanded;
    
    public ConnectionListToolViewModel ParentViewModel { get; }
    public ImageUri Icon => new ImageUri("Icons/icon_mini_schema_big.png");
    public string SchemaName { get; }
    
    private ObservableCollectionExtended<IChildType> tables = new ObservableCollectionExtended<IChildType>();

    private CancellationTokenSource? loadToken;
    
    public SchemaViewModel(ConnectionListToolViewModel parentViewModel,
        DatabaseCredentials credentials,
        string schemaName)
    {
        this.credentials = credentials;
        ParentViewModel = parentViewModel;
        SchemaName = schemaName;
        
        tables.CollectionChanged += TablesOnCollectionChanged;
        On(() => IsExpanded, @is =>
        {
            loadToken?.Cancel();
            if (@is)
            {
                loadToken = new CancellationTokenSource();
                LoadTablesAsync(loadToken.Token).ListenErrors(ParentViewModel.MessageBoxService);
            }
            else
                loadToken = null;
        });
    }
    
    private void TablesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ChildrenChanged?.Invoke(this, e);
    }

    private async Task LoadTablesAsync(CancellationToken token)
    {
        tables.RemoveAll();
        tables.Add(new RowLoadingViewModel(this));

        IReadOnlyList<string> tableNames = Array.Empty<string>();
        try
        {
            await using var session = await ParentViewModel.SqlConnector.ConnectAsync(credentials.WithSchemaName(SchemaName));
            
            if (token.IsCancellationRequested)
                return;
            
            tableNames = await session.GetTablesAsync(token);

            if (token.IsCancellationRequested)
                return;
        }
        finally
        {
            tables.RemoveAll();
        }

        var viewModels = tableNames
            .Select(x => new TableViewModel(this, SchemaName, x))
            .ToList();
        
        tables.RemoveAll();
        tables.AddRange(viewModels);
        ParentViewModel.Refilter();
    }

    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
    public IReadOnlyList<IParentType> NestedParents => Array.Empty<IParentType>();
    public IReadOnlyList<IChildType> Children => tables;
    public event NotifyCollectionChangedEventHandler? ChildrenChanged;
    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;
}