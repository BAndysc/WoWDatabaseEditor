using System.Collections.ObjectModel;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Events;

namespace WDE.Solutions.Explorer.ViewModels
{
    public class AddItemCategoryMenuViewModel : BindableBase
    {
        public string Name { get; }
        public ObservableCollection<SolutionItemMenuViewModel> Items { get; } = new();

        public AddItemCategoryMenuViewModel(string groupName)
        {
            Name = groupName;
        }
    }

    public class SolutionItemMenuViewModel : BindableBase
    {
        public string Name { get; }
        public ICommand Command { get; }
        
        public SolutionItemMenuViewModel(ISolutionItemProvider provider, 
            ISolutionManager solutionManager,
            IEventAggregator eventAggregator)
        {
            Name = provider.GetName();

            Command = new AsyncCommand(async () =>
            {
                var item = await provider.CreateSolutionItem();
                if (item != null)
                {
                    solutionManager.Items.Add(item);
                    if (item is not SolutionFolderItem)
                        eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
                }
            });
        }
    }
}