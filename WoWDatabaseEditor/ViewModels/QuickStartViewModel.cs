using System.Collections.ObjectModel;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Events;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WoWDatabaseEditorCore.Services.NewItemService;

namespace WoWDatabaseEditorCore.ViewModels
{
    public class QuickStartViewModel : ObservableBase, IDocument
    {
        private readonly ISolutionItemIconRegistry iconRegistry;
        private readonly ISolutionItemNameRegistry nameRegistry;
        private readonly IMostRecentlyUsedService mostRecentlyUsedService;
        public AboutViewModel AboutViewModel { get; }
        public ObservableCollection<NewItemPrototypeInfo> FlatItemPrototypes { get; } = new();
        public ObservableCollection<MostRecentlyUsedViewModel> MostRecentlyUsedItems { get; } = new();
        
        public QuickStartViewModel(ISolutionItemProvideService solutionItemProvideService, 
            IEventAggregator eventAggregator,
            ISolutionItemIconRegistry iconRegistry,
            ISolutionItemNameRegistry nameRegistry,
            ICurrentCoreVersion currentCoreVersion,
            IMainThread mainThread,
            IMostRecentlyUsedService mostRecentlyUsedService,
            AboutViewModel aboutViewModel)
        {
            this.iconRegistry = iconRegistry;
            this.nameRegistry = nameRegistry;
            this.mostRecentlyUsedService = mostRecentlyUsedService;
            AboutViewModel = aboutViewModel;
            foreach (var item in solutionItemProvideService.AllCompatible)
            {
                if (item.IsContainer || !item.ShowInQuickStart(currentCoreVersion.Current))
                    continue;
                
                var info = new NewItemPrototypeInfo(item);

                if (info.RequiresName)
                    continue;
                FlatItemPrototypes.Add(info);
            }

            LoadItemCommand = new AsyncAutoCommand<NewItemPrototypeInfo>(async prototype =>
            {
                var item = await prototype.CreateSolutionItem("");
                if (item != null)
                    eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
            });

            OpenMostRecentlyUsedCommand = new AsyncAutoCommand<MostRecentlyUsedViewModel>(async item =>
            {
                eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item.Item);
            });

            AutoDispose(eventAggregator.GetEvent<EventRequestOpenItem>().Subscribe(item =>
            {
                mainThread.Dispatch(ReloadMruList);
            }, true));

            ReloadMruList();
        }

        private void ReloadMruList()
        {
            MostRecentlyUsedItems.Clear();
            foreach (var mru in mostRecentlyUsedService.MostRecentlyUsed)
            {
                var vm = new MostRecentlyUsedViewModel(iconRegistry.GetIcon(mru), nameRegistry.GetName(mru), mru);
                MostRecentlyUsedItems.Add(vm);
            }
        }

        public AsyncAutoCommand<NewItemPrototypeInfo> LoadItemCommand { get; }
        public AsyncAutoCommand<MostRecentlyUsedViewModel> OpenMostRecentlyUsedCommand { get; }
        
        public ImageUri? Icon => new ImageUri("Icons/wde_icon.png");
        public string Title => "Quick start";
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => AlwaysDisabledCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
    }

    public class MostRecentlyUsedViewModel
    {
        public ImageUri Icon { get; }
        public string Name { get; }
        public ISolutionItem Item { get; }

        public MostRecentlyUsedViewModel(ImageUri icon, string name, ISolutionItem item)
        {
            Icon = icon;
            Name = name;
            Item = item;
        }
    }
}