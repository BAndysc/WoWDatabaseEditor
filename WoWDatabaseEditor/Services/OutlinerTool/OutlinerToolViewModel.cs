using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Outliner;
using WDE.Common.Parameters;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Services.OutlinerTool;


[AutoRegister]
[SingleInstance]
public partial class OutlinerToolViewModel : ObservableBase, ITool
{
    private readonly Lazy<IDocumentManager> documentManager;
    private readonly Lazy<ISolutionItemRelatedRegistry> relatedRegistry;
    private readonly Lazy<IFindAnywhereService> findAnywhereService;
    private readonly IEventAggregator eventAggregator;
    private ObservableCollection<OutlinerGroupViewModel> groups = new();
    public ObservableCollection<OutlinerGroupViewModel> Groups => groups;

    private FlatTreeList<OutlinerGroupViewModel, OutlinerItemViewModel> flatItems;
    public FlatTreeList<OutlinerGroupViewModel, OutlinerItemViewModel> FlatItems => flatItems;
    
    private Dictionary<RelatedSolutionItem.RelatedType, OutlinerGroupViewModel> groupsByType = new();
    private OutlinerGroupViewModel referencedBy = new("Used in");
    
    private System.IDisposable? currentSubscription;
    private CancellationTokenSource? currentCancellationTokenSource;
    
    public OutlinerToolViewModel(Lazy<IDocumentManager> documentManager,
        Lazy<ISolutionItemRelatedRegistry> relatedRegistry,
        Lazy<IFindAnywhereService> findAnywhereService,
        IEventAggregator eventAggregator)
    {
        this.documentManager = documentManager;
        this.relatedRegistry = relatedRegistry;
        this.findAnywhereService = findAnywhereService;
        this.eventAggregator = eventAggregator;
        groupsByType[RelatedSolutionItem.RelatedType.CreatureEntry] = new OutlinerGroupViewModel("Creatures");
        groupsByType[RelatedSolutionItem.RelatedType.GameobjectEntry] = new OutlinerGroupViewModel("Game Objects");
        groupsByType[RelatedSolutionItem.RelatedType.QuestEntry] = new OutlinerGroupViewModel("Quests");
        groupsByType[RelatedSolutionItem.RelatedType.Template] = new OutlinerGroupViewModel("Templates");
        groupsByType[RelatedSolutionItem.RelatedType.TimedActionList] = new OutlinerGroupViewModel("Timed Action Lists");
        
        foreach (var group in groupsByType.Values)
            groups.Add(group);
        groups.Add(referencedBy);

        flatItems = new FlatTreeList<OutlinerGroupViewModel, OutlinerItemViewModel>(groups);

        AutoDispose(eventAggregator.GetEvent<AllModulesLoaded>().Subscribe(Bind, true));
    }
    
    private void Bind()
    {
        documentManager.Value.ToObservable(d => d.ActiveDocument).SubscribeAction(active =>
        {
            currentSubscription?.Dispose();
            currentSubscription = null;
            ClearGroups();
            ClearReferences();

            if (active is ISolutionItemDocument document)
            {
                if (document is IOutlinerSourceDocument outlinerSource)
                    currentSubscription = outlinerSource.OutlinerModel.SubscribeAction(Update);

                if (currentCancellationTokenSource != null)
                {
                    currentCancellationTokenSource.Cancel();
                    currentCancellationTokenSource = null;
                }
                
                RefreshReferencedBy(document).ListenErrors();
            }
        });
    }

    private async Task RefreshReferencedBy(ISolutionItemDocument document)
    {
        ClearReferences();
        var related = await relatedRegistry.Value.GetRelated(document.SolutionItem);
        if (related == null)
            return;
        
        string type;
        if (related.Value.Type == RelatedSolutionItem.RelatedType.CreatureEntry)
            type = "CreatureParameter";
        else if (related.Value.Type == RelatedSolutionItem.RelatedType.TimedActionList)
            type = "TimedActionListParameter";
        else if (related.Value.Type == RelatedSolutionItem.RelatedType.GameobjectEntry)
            type = "GameobjectParameter";
        else if (related.Value.Type == RelatedSolutionItem.RelatedType.Template)
            type = "AITemplateParameter";
        else if (related.Value.Type == RelatedSolutionItem.RelatedType.QuestEntry)
            type = "QuestParameter";
        else
            return;
        var token = new CancellationTokenSource();
        currentCancellationTokenSource = token;
        referencedBy.IsRefreshing = true;
        await findAnywhereService.Value.Find(new FindRelatedContext(this, token), new[] { type }, new[] { related.Value.Entry }, token.Token);
        if (token == currentCancellationTokenSource)
            currentCancellationTokenSource = null;
        referencedBy.IsRefreshing = false;
        UpdateGroupsVisibility();
    }

    private class FindRelatedContext : IFindAnywhereResultContext
    {
        private readonly OutlinerToolViewModel that;
        private readonly CancellationTokenSource token;

        public FindRelatedContext(OutlinerToolViewModel that, CancellationTokenSource token)
        {
            this.that = that;
            this.token = token;
        }

        public void AddResult(IFindAnywhereResult result)
        {
            if (token != that.currentCancellationTokenSource)
                return;
            
            ICommand? customCommand = result.CustomCommand;
            if (customCommand != null)
            {
                customCommand = new DelegateCommand<OutlinerItemViewModel>(_ =>
                {
                    result.CustomCommand!.Execute(result);
                }); 
            }
            that.AddReferencedBy(result.SolutionItem, result.Entry, result.Title, result.Icon, customCommand);
        }
    }
    
    public string Title => "Outliner";
    public string UniqueId => "outliner";
    [Notify] private bool visibility;
    public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Right;
    public bool OpenOnStart => false;
    [Notify] private bool isSelected;

    private void ClearGroups()
    {
        foreach (var group in groups)
        {
            if (group != referencedBy)
                group.Clear();
        }
    }

    private void UpdateGroupsVisibility()
    {
        foreach (var group in groups)
        {
            group.IsVisible = group.Items.Count > 0;
        }
    }
    
    public void Update(IOutlinerModel model)
    {
        ClearGroups();
        foreach (var (type, element) in model)
        {
            var vm = new OutlinerItemViewModel(element.SolutionItem, element.Entry, element.Title, element.Icon, element.CustomCommand);
            groupsByType[type].Add(vm);
        }

        UpdateGroupsVisibility();
    }

    public void ClearReferences()
    {
        referencedBy.Clear();
        UpdateGroupsVisibility();
    }

    public void AddReferencedBy(ISolutionItem? solutionItem, long? entry, string? name, ImageUri icon, ICommand? customCommand)
    {
        referencedBy.Add(new OutlinerItemViewModel(solutionItem, entry, name, icon, customCommand));
        if (!referencedBy.IsVisible)
            referencedBy.IsVisible = true;
    }

    public void Open(OutlinerItemViewModel viewModel)
    {
        if (viewModel.CustomCommand is { })
            viewModel.CustomCommand?.Execute(viewModel);
        else
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(viewModel.SolutionItem!);
    }
}

public partial class OutlinerGroupViewModel : IParentType
{
    [Notify] private bool isExpanded = true;
    private string name;
    public string Name { get; private set; }

    private bool isRefreshing;
    public bool IsRefreshing
    {
        get => isRefreshing;
        set
        {
            if (isRefreshing == value)
                return;
            isRefreshing = value;
            if (value)
                Name = name + " (refreshing...)";
            else
                Name = name;
        }
    }
    
    public ObservableCollection<OutlinerItemViewModel> Items { get; } = new();

    public OutlinerGroupViewModel(string name)
    {
        this.name = name;
        Name = name;
        Items.CollectionChanged += ItemsOnCollectionChanged;
    }

    private void ItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ChildrenChanged?.Invoke(this, e);
    }

    public void Clear()
    {
        Items.RemoveAll();
    }

    public void Add(OutlinerItemViewModel item)
    {
        Items.Add(item);
    }

    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } = true;
    public IParentType? Parent { get; set; }
    public IReadOnlyList<IParentType> NestedParents => Array.Empty<IParentType>();
    public IReadOnlyList<IChildType> Children => Items;

    public event NotifyCollectionChangedEventHandler? ChildrenChanged;
    public event NotifyCollectionChangedEventHandler? NestedParentsChanged;
}

public partial class OutlinerItemViewModel : IChildType
{
    public OutlinerItemViewModel(ISolutionItem? solutionItem, long? entry, string? name, ImageUri icon, ICommand? customCommand)
    {
        Name = name;
        Icon = icon;
        Entry = entry?.ToString();
        CustomCommand = customCommand;
        SolutionItem = solutionItem;
    }

    public ISolutionItem? SolutionItem { get; }
    public string? Name { get; }
    public ImageUri Icon { get; }
    public string? Entry { get; }
    public ICommand? CustomCommand { get; }
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } = true;
    public IParentType? Parent { get; set; }
}