using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Outliner;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Services.OutlinerTool;

[SingleInstance]
[AutoRegister]
public partial class OutlinerToolService : ObservableBase, IOutlinerToolService
{
    [Notify] private IOutlinerItemViewModel? selectedItem;

    public OutlinerToolService()
    {
        
    }
}

[AutoRegister]
[SingleInstance]
public partial class OutlinerToolViewModel : ObservableBase, ITool
{
    private readonly Lazy<IDocumentManager> documentManager;
    private readonly Lazy<ISolutionItemRelatedRegistry> relatedRegistry;
    private readonly Lazy<IFindAnywhereService> findAnywhereService;
    private readonly IOutlinerSettingsService settings;
    private readonly OutlinerToolService outlinerService;
    private readonly IEventAggregator eventAggregator;
    private ObservableCollection<OutlinerGroupViewModel> groups = new();
    public ObservableCollection<OutlinerGroupViewModel> Groups => groups;

    private FlatTreeList<OutlinerGroupViewModel, OutlinerItemViewModel> flatItems;
    public FlatTreeList<OutlinerGroupViewModel, OutlinerItemViewModel> FlatItems => flatItems;
    
    private Dictionary<RelatedSolutionItem.RelatedType, OutlinerGroupViewModel> groupsByType = new();
    private OutlinerGroupViewModel referencedBy = new("Used in");
    
    private System.IDisposable? currentSubscription;
    private CancellationTokenSource? currentCancellationTokenSource;
    
    private INodeType? selectedNode;
    public INodeType? SelectedNode
    {
        get => selectedNode;
        set
        {
            SetProperty(ref selectedNode, value);
            outlinerService.SelectedItem = value as IOutlinerItemViewModel;
        }
    }
    
    public ObservableCollection<OutlinerSourceViewModel> Sources { get; } = new();

    public OutlinerToolViewModel(Lazy<IDocumentManager> documentManager,
        Lazy<ISolutionItemRelatedRegistry> relatedRegistry,
        Lazy<IFindAnywhereService> findAnywhereService,
        IOutlinerSettingsService settings,
        OutlinerToolService outlinerService,
        IEventAggregator eventAggregator)
    {
        this.documentManager = documentManager;
        this.relatedRegistry = relatedRegistry;
        this.findAnywhereService = findAnywhereService;
        this.settings = settings;
        this.outlinerService = outlinerService;
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

        foreach (var val in Enum.GetValues<FindAnywhereSourceType>())
        {
            if (val != FindAnywhereSourceType.None && val != FindAnywhereSourceType.All)
                Sources.Add(new OutlinerSourceViewModel(val, settings));
        }

        foreach (var source in Sources)
        {
            source.PropertyChanged += (_, _) => AnalyseDocument(documentManager.Value.ActiveDocument);
        }
        
        On(() => Visibility, @is =>
        {
            if (@is)
                AnalyseDocument(documentManager.Value.ActiveDocument);
            else
                AnalyseDocument(null);
        });
        
        AutoDispose(eventAggregator.GetEvent<AllModulesLoaded>().Subscribe(Bind, true));
    }

    private void AnalyseDocument(IDocument? document)
    {
        currentSubscription?.Dispose();
        currentSubscription = null;
        if (currentCancellationTokenSource != null)
        {
            currentCancellationTokenSource.Cancel();
            currentCancellationTokenSource = null;
        }
        ClearGroups();
        ClearReferences();

        if (document is ISolutionItemDocument solutionDocument)
        {
            if (document is IOutlinerSourceDocument outlinerSource)
                currentSubscription = outlinerSource.OutlinerModel.SubscribeAction(Update);

            currentCancellationTokenSource = new CancellationTokenSource();
            RefreshReferencedBy(solutionDocument, currentCancellationTokenSource.Token).ListenErrors();
        }
    }
    
    private void Bind()
    {
        documentManager.Value.ToObservable(d => d.ActiveDocument).SubscribeAction(active =>
        {
            if (visibility)
                AnalyseDocument(active);
        });
    }

    private async Task RefreshReferencedBy(ISolutionItemDocument document, CancellationToken token)
    {
        ClearReferences();
        var related = await relatedRegistry.Value.GetRelated(document.SolutionItem);
        if (related == null)
            return;

        if (token.IsCancellationRequested)
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
        else if (related.Value.Type == RelatedSolutionItem.RelatedType.Spell)
            type = "SpellParameter";
        else
            return;

        referencedBy.IsRefreshing = true;
        await findAnywhereService.Value.Find(new FindRelatedContext(this, token), FindAnywhereSourceType.All &~ settings.SkipSources, new[] { type }, new[] { related.Value.Entry }, token);

        if (token.IsCancellationRequested)
            return;

        referencedBy.IsRefreshing = false;
        UpdateGroupsVisibility();
    }

    private class FindRelatedContext : IFindAnywhereResultContext
    {
        private readonly OutlinerToolViewModel that;
        private readonly CancellationToken token;

        public FindRelatedContext(OutlinerToolViewModel that, CancellationToken token)
        {
            this.that = that;
            this.token = token;
        }

        public void AddResult(IFindAnywhereResult result)
        {
            if (token.IsCancellationRequested)
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