using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Types;

namespace WoWDatabaseEditorCore.Services.NewItemService
{
    public interface INewItemDialogViewModel : IDialog
    {
        ObservableCollection<NewItemPrototypeGroup> Categories { get; }
        NewItemPrototypeInfo? SelectedPrototype { get; }
        Task<ISolutionItem?> CreateSolutionItem();
        void AllowFolders(bool showFolders);
    }

    public class NewItemPrototypeInfo
    {
        private readonly ISolutionItemProvider provider;

        public NewItemPrototypeInfo(ISolutionItemProvider provider, bool enabled, bool forceNewLine = false)
        {
            this.provider = provider;
            Name = provider.GetName();
            Description = provider.GetDescription();
            Image = provider.GetImage();
            IsContainer = provider.IsContainer;
            RequiresName = provider is INamedSolutionItemProvider;
            IsEnabled = enabled;
            ForceNewLine = forceNewLine;
        }

        public string Name { get; }
        public string Description { get; }
        public ImageUri Image { get; }
        public bool RequiresName { get; }
        public bool IsContainer { get; }
        public bool IsEnabled { get; }
        public bool ForceNewLine { get; }

        public Task<ISolutionItem?> CreateSolutionItem(string name)
        {
            if (provider is INamedSolutionItemProvider namedSolutionItemProvider)
                return namedSolutionItemProvider.CreateSolutionItem(name);
            return provider.CreateSolutionItem();
        }
    }
}