using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using WDE.Common;
using Prism.Ioc;
using Prism.Events;
using WDE.Common.Events;
using WDE.Common.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionManager : ISolutionManager
    {
        private ObservableCollection<ISolutionItem> _items;
        public ObservableCollection<ISolutionItem> Items => _items;

        public SolutionManager(IEventAggregator eventAggregator)
        {
            _items = new ObservableCollection<ISolutionItem>();

            Initialize();

            _items.CollectionChanged += ItemsOnCollectionChanged;
        }

        private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            var newItems = notifyCollectionChangedEventArgs.NewItems;

            foreach (var varitem in newItems)
            {
                var item = varitem as ISolutionItem;

                if (item.Items != null)
                    item.Items.CollectionChanged += ItemsOnCollectionChanged;
            }

            Save();
        }

        private void Save()
        {
            JsonSerializer ser = new Newtonsoft.Json.JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
            using (StreamWriter file = File.CreateText(@"solutions.json"))
            {
                ser.Serialize(file, _items);
            }
        }

        public void Initialize()
        {
            if (System.IO.File.Exists("solutions.json"))
            {
                JsonSerializer ser = new Newtonsoft.Json.JsonSerializer() {TypeNameHandling = TypeNameHandling.Auto};
                using (StreamReader re = new StreamReader("solutions.json"))
                {
                    JsonTextReader reader = new JsonTextReader(re);
                    ser.Deserialize<List<ISolutionItem>>(reader).ForEach((e) =>
                    {
                        InitItem(e);
                        _items.Add(e);
                    });
                }
            }
        }

        private void InitItem(ISolutionItem item)
        {
            if (item.Items != null)
                item.Items.CollectionChanged += ItemsOnCollectionChanged;
            //@todo fixme
//            item.SetUnity(_container);
            if (item.Items != null)
            {
                foreach (var iitem in item.Items)
                {
                    InitItem(iitem);
                }
            }
        }
    }
}
