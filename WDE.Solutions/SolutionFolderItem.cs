using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using WDE.Common;

namespace WDE.Solutions
{
    public class SolutionFolderItem : ISolutionItem
    {
        public SolutionFolderItem(string name, IEnumerable<ISolutionItem> children)
        {
            MyName = name;
            Items = new ObservableCollection<ISolutionItem>(children);
        }
        
        public SolutionFolderItem(string name)
        {
            MyName = name;
            Items = new ObservableCollection<ISolutionItem>();
        }

        public SolutionFolderItem()
        {
            MyName = "";
            Items = new ObservableCollection<ISolutionItem>();
        }

        public string MyName { get; set; }

        [JsonIgnore]
        public bool IsContainer => true;

        public ObservableCollection<ISolutionItem> Items { get; }

        [JsonIgnore]
        public string? ExtraId => null;

        [JsonIgnore]
        public bool IsExportable => true;

        public void AddItem(ISolutionItem item)
        {
            Items.Add(item);
        }

        public ISolutionItem Clone() => new SolutionFolderItem(MyName, Items.Select(i => i.Clone()));
    }
}