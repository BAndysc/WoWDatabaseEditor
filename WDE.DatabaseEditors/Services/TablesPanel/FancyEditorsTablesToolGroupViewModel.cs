using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Documents;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.ViewModels;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.DatabaseEditors.Services.TablesPanel;

[AutoRegister]
[SingleInstance]
public partial class FancyEditorsTablesToolGroupViewModel : ObservableBase, ITablesToolGroup
{
    private readonly ITableDefinitionProvider definitionProvider;
    private readonly ISolutionItemProvideService rawTableSolutionItemProviderService;
    private readonly ITableOpenService tableOpenService;
    private readonly IEventAggregator eventAggregator;
    private readonly IMessageBoxService messageBoxService;
    private readonly ITableDefinitionEditorService definitionEditorService;
    private List<TableItemViewModel> allTables = new();
    private string searchText = "";
    [Notify] private TableItemViewModel? selectedTable;
    public ImageUri Icon => new ImageUri("Icons/icon_tables.png");
    public string GroupName => "Editors";
    public RgbColor? CustomColor => null;
    public int Priority => 100;
    
    public void ToolOpened()
    {
    }

    public void ToolClosed()
    {
    }

    public ObservableCollection<TableItemViewModel> FilteredTables { get; } = new();

    public ICommand EditDefinitionCommand { get; }

    public FancyEditorsTablesToolGroupViewModel(ITableDefinitionProvider definitionProvider,
        ISolutionItemProvideService rawTableSolutionItemProviderService,
        ITableOpenService tableOpenService,
        IEventAggregator eventAggregator,
        IMessageBoxService messageBoxService,
        ITableDefinitionEditorService definitionEditorService)
    {
        this.definitionProvider = definitionProvider;
        this.rawTableSolutionItemProviderService = rawTableSolutionItemProviderService;
        this.tableOpenService = tableOpenService;
        this.eventAggregator = eventAggregator;
        this.messageBoxService = messageBoxService;
        this.definitionEditorService = definitionEditorService;
        definitionProvider.DefinitionsChanged += UpdateDefinitions;
        UpdateDefinitions();

        EditDefinitionCommand = new DelegateCommand<TableItemViewModel>(item =>
        {
            if (item.Definition != null)
                definitionEditorService.EditDefinition(item.Definition.AbsoluteFileName);
        }, item => item != null && item.Definition != null);

        On(() => SearchText, FilterTables);
    }

    private void FilterTables(string search)
    {
        FilteredTables.Clear();
        if (string.IsNullOrEmpty(search))
        {
            FilteredTables.AddRange(allTables);
        }
        else
        {
            search = search.ToLower();
            foreach (var table in allTables)
            {
                if (table.TableName.Contains(search, StringComparison.Ordinal))
                {
                    FilteredTables.Add(table);
                }
            }
        }
    }

    private void UpdateDefinitions()
    {
        allTables.Clear();

        foreach (var defi in definitionProvider.Definitions)
        {
            allTables.Add(new TableItemViewModel(defi.TableName, defi));
            if (defi.ForeignTable != null)
            {
                foreach (var foreign in defi.ForeignTable)
                {
                    allTables.Add(new TableItemViewModel(foreign.TableName, defi));
                }
            }
        }

        foreach (var provider in rawTableSolutionItemProviderService.AllCompatible)
        {
            if (provider is IRawDatabaseTableSolutionItemProvider rawTable)
            {
                allTables.Add(new TableItemViewModel(rawTable.TableName, provider));
            }
        }

        allTables.Sort((a, b) => String.Compare(a.TableName, b.TableName, StringComparison.Ordinal));

        FilterTables(searchText);
    }

    public void OpenTable(TableItemViewModel item)
    {
        DoOpenTable(item).ListenErrors(messageBoxService);
    }

    public bool OpenSelected()
    {
        if (selectedTable != null)
        {
            DoOpenTable(selectedTable).ListenErrors(messageBoxService);
            return true;
        }
        else if (FilteredTables.Count == 1)
        {
            DoOpenTable(FilteredTables[0]).ListenErrors(messageBoxService);
            return true;
        }

        return false;
    }
    
    private async Task DoOpenTable(TableItemViewModel item)
    {
        ISolutionItem? solution;

        if (item.Definition is { } definition)
            solution = await tableOpenService.TryCreate(definition);
        else if (item.Provider is { } provider)
            solution = await provider.CreateSolutionItem();
        else
            throw new Exception("Internal Editor error: Invalid table item (neither a table definition nor a solution item provider, report on github)");

        if (solution != null)
        {
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(solution);
        }
    }
    
    public string SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value);
    }
}

public class TableItemViewModel : ObservableBase
{
    public ImageUri Icon { get; }
    public string TableName { get; }
    public DatabaseTableDefinitionJson? Definition { get; }
    public ISolutionItemProvider? Provider { get; }

    public TableItemViewModel(string tableName, DatabaseTableDefinitionJson definition)
    {
        TableName = tableName;
        Definition = definition;
        Provider = null;
        Icon = new ImageUri(definition.IconPath ?? "Icons/document.png");
    }
    
    public TableItemViewModel(string tableName, ISolutionItemProvider provider)
    {
        TableName = tableName;
        Definition = null;
        Provider = provider;
        Icon = provider.GetImage();
    }
}