using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Types;

namespace WoWDatabaseEditorCore.Services.NewItemService
{
    public interface INewItemDialogViewModel : IDialog
    {
        ObservableCollection<NewItemPrototypeGroup> ItemPrototypes { get; }
        NewItemPrototypeInfo? SelectedPrototype { get; }
        Task<ISolutionItem?> CreateSolutionItem();
    }

    public class NewItemPrototypeInfo
    {
        private readonly ISolutionItemProvider provider;

        public NewItemPrototypeInfo(ISolutionItemProvider provider)
        {
            this.provider = provider;
            Name = provider.GetName();
            Description = provider.GetDescription();
            Image = provider.GetImage();
            RequiresName = provider is INamedSolutionItemProvider;
        }

        public string Name { get; }
        public string Description { get; }
        public ImageUri Image { get; }
        public bool RequiresName { get; }

        public Task<ISolutionItem> CreateSolutionItem(string name)
        {
            if (provider is INamedSolutionItemProvider namedSolutionItemProvider)
                return namedSolutionItemProvider.CreateSolutionItem(name);
            return provider.CreateSolutionItem();
        }
    }
}