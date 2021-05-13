using System.Collections.ObjectModel;
using WDE.Common;

namespace WDE.Solutions
{
    public class SolutionFolderItem : ISolutionItem
    {
        public SolutionFolderItem(string name)
        {
            MyName = name;
            Items = new ObservableCollection<ISolutionItem>();
        }

        public string MyName { get; set; }

        public bool IsContainer => true;

        public ObservableCollection<ISolutionItem> Items { get; }

        public string? ExtraId => null;

        public bool IsExportable => true;

        public void AddItem(ISolutionItem item)
        {
            Items.Add(item);
        }
    }
}