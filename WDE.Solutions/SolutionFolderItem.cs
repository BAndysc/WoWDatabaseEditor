using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WDE.Common;
using Prism.Ioc;

namespace WDE.Solutions
{
    public class SolutionFolderItem : ISolutionItem
    {
        public string MyName { get; set; }

        public string Name => MyName;

        public bool IsContainer => true;

        public ObservableCollection<ISolutionItem> Items => _items;

        private ObservableCollection<ISolutionItem> _items;

        public SolutionFolderItem(string name)
        {
            MyName = name;
            _items = new ObservableCollection<ISolutionItem>();
        }

        public void AddItem(ISolutionItem item)
        {
            _items.Add(item);
        }

        public string ExtraId => null;

        public bool IsExportable => true;

        public string ExportSql
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in Items)
                    if (item.IsExportable)
                        sb.AppendLine(item.ExportSql);
                return sb.ToString();
            }
        }
    }
}
