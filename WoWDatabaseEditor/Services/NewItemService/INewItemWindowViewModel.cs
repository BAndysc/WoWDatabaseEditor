using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using WDE.Common;

namespace WoWDatabaseEditor.Services.NewItemService
{
    public interface INewItemWindowViewModel
    {
        ObservableCollection<NewItemPrototypeInfo> ItemPrototypes { get; }
        NewItemPrototypeInfo? SelectedPrototype { get;  }
    }

    public class NewItemPrototypeInfo
    {
        private readonly ISolutionItemProvider _provider;
        public string Name { get; private set; }
        public string Description { get; private set; }
        public ImageSource Image { get; private set; }

        public NewItemPrototypeInfo(ISolutionItemProvider provider)
        {
            _provider = provider;
            Name = provider.GetName();
            Description = provider.GetDescription();
            Image = provider.GetImage();
        }

        public ISolutionItem CreateSolutionItem()
        {
            return _provider.CreateSolutionItem();
        }
    }
}
