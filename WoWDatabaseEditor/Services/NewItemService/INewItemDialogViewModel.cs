using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Types;

namespace WoWDatabaseEditorCore.Services.NewItemService
{
    public interface INewItemDialogViewModel : IDialog
    {
        ObservableCollection<NewItemPrototypeInfo> ItemPrototypes { get; }
        NewItemPrototypeInfo? SelectedPrototype { get; }
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
        }

        public string Name { get; }
        public string Description { get; }
        public ImageUri Image { get; }

        public Task<ISolutionItem> CreateSolutionItem()
        {
            return provider.CreateSolutionItem();
        }
    }
}