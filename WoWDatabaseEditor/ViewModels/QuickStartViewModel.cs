using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Events;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WoWDatabaseEditorCore.Services.NewItemService;

namespace WoWDatabaseEditorCore.ViewModels
{
    public class QuickStartViewModel : ObservableBase, IDocument
    {
        private readonly IEventAggregator eventAggregator;
        public AboutViewModel AboutViewModel { get; }
        public ObservableCollection<NewItemPrototypeInfo> FlatItemPrototypes { get; } = new();
        
        public QuickStartViewModel(ISolutionItemProvideService solutionItemProvideService, 
            IEventAggregator eventAggregator,
            ICurrentCoreVersion currentCoreVersion,
            AboutViewModel aboutViewModel)
        {
            this.eventAggregator = eventAggregator;
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
        }
        
        public AsyncAutoCommand<NewItemPrototypeInfo> LoadItemCommand { get; }
        
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
}