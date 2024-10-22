using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Events;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.ViewModels
{
    public class RelatedSolutionItems : ObservableBase
    {
        private readonly IDocumentManager documentManager;
        private readonly ISolutionItemProvideService provideService;
        private readonly ISolutionItemRelatedRegistry relatedRegistry;
        private readonly IEventAggregator eventAggregator;
        private readonly IWindowManager windowManager;
        private readonly ICurrentCoreVersion currentCoreVersion;
        public ObservableCollection<RelatedSolutionItemViewModel> List { get; } = new();

        private CancellationTokenSource? tokenSource;
        
        public RelatedSolutionItems(IDocumentManager documentManager,
            ISolutionItemProvideService provideService,
            ISolutionItemRelatedRegistry relatedRegistry,
            IEventAggregator eventAggregator,
            ISolutionManager solutionManager,
            IWindowManager windowManager,
            ICurrentCoreVersion currentCoreVersion)
        {
            this.documentManager = documentManager;
            this.provideService = provideService;
            this.relatedRegistry = relatedRegistry;
            this.eventAggregator = eventAggregator;
            this.windowManager = windowManager;
            this.currentCoreVersion = currentCoreVersion;
            solutionManager.RefreshRequest += _ => DoProcess(documentManager.ActiveSolutionItemDocument);
            documentManager.ToObservable(t => t.ActiveSolutionItemDocument).SubscribeAction(DoProcess);
            eventAggregator.GetEvent<DatabaseCacheReloaded>()
                .Subscribe(_ => DoProcess(documentManager.ActiveSolutionItemDocument), ThreadOption.UIThread, true);
        }

        private void DoProcess(ISolutionItemDocument? item)
        {
            tokenSource?.Cancel();
            tokenSource = new CancellationTokenSource();
            Process(item?.SolutionItem, tokenSource.Token).ListenErrors();
        }

        private async Task Process(ISolutionItem? item, CancellationToken token)
        {
            List.Clear();

            if (item == null)
                return;
            
            var related = await relatedRegistry.GetRelated(item);
            if (related == null)
                return;

            foreach (var provider in provideService.GetRelatedCreators(related.Value))
            {
                if (!provider.ShowInQuickStart(currentCoreVersion.Current))
                    continue;

                if (token.IsCancellationRequested)
                    return;
                
                List.Add(new RelatedSolutionItemViewModel(provider.GetName(), provider.GetImage(), new AsyncAutoCommand(async () =>
                {
                    var newItem = await provider.CreateRelatedSolutionItem(related.Value);
                    if (newItem == null)
                        return;
                    eventAggregator.GetEvent<EventRequestOpenItem>().Publish(newItem);
                })));
            }

            if (related.Value.Type == RelatedSolutionItem.RelatedType.CreatureEntry ||
                related.Value.Type == RelatedSolutionItem.RelatedType.GameobjectEntry ||
                related.Value.Type == RelatedSolutionItem.RelatedType.QuestEntry)
            {
                List.Add(new RelatedSolutionItemViewModel("Open wowhead in a browser", new ImageUri("Icons/icon_head_red.png"), new AsyncAutoCommand(async () =>
                {
                    var type = related.Value.Type == RelatedSolutionItem.RelatedType.CreatureEntry ? "npc" : "object";
                    if (related.Value.Type == RelatedSolutionItem.RelatedType.QuestEntry)
                        type = "quest";
                    windowManager.OpenUrl($"https://wowhead.com/{type}=" + related.Value.Entry);
                })));
            }
        }
    }
        
    public class RelatedSolutionItemViewModel
    {
        public RelatedSolutionItemViewModel(string name, ImageUri icon, ICommand createCommand)
        {
            Name = name;
            Icon = icon.Uri?.Contains("_big") ?? false ? new ImageUri(icon.Uri.Replace("_big", "")) : icon;
            CreateCommand = createCommand;
        }

        public string Name { get; }
        public ImageUri Icon { get; }
        public ICommand CreateCommand { get; }
    }
}