using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using Newtonsoft.Json;
using Prism.Events;
using WDE.Common;
using WDE.Module.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    [SingleInstance]
    public class SolutionManager : ISolutionManager
    {
        public SolutionManager(IEventAggregator eventAggregator)
        {
            Items = new ObservableCollection<ISolutionItem>();

            Initialize();

            Items.CollectionChanged += ItemsOnCollectionChanged;
        }

        public ObservableCollection<ISolutionItem> Items { get; }

        public void Initialize()
        {
            if (File.Exists("solutions.json"))
            {
                JsonSerializer ser = new() {TypeNameHandling = TypeNameHandling.Auto};
                using (StreamReader re = new("solutions.json"))
                {
                    JsonTextReader reader = new(re);
                    ser.Deserialize<List<ISolutionItem>>(reader)
                        .ForEach(e =>
                        {
                            InitItem(e);
                            Items.Add(e);
                        });
                }
            }
        }

        private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            IList? newItems = notifyCollectionChangedEventArgs.NewItems;
            IList? oldItems = notifyCollectionChangedEventArgs.OldItems;

            if (newItems != null)
            {
                foreach (object varitem in newItems)
                {
                    ISolutionItem item = varitem as ISolutionItem;

                    if (item.Items != null)
                        item.Items.CollectionChanged += ItemsOnCollectionChanged;
                }
            }

            if (oldItems != null)
            {
                foreach (object varitem in oldItems)
                {
                    ISolutionItem item = varitem as ISolutionItem;

                    if (item.Items != null)
                        item.Items.CollectionChanged -= ItemsOnCollectionChanged;
                }
            }

            Save();
        }

        private void Save()
        {
            JsonSerializer ser = new() {TypeNameHandling = TypeNameHandling.Auto};
            using (StreamWriter file = File.CreateText(@"solutions.json"))
            {
                ser.Serialize(file, Items);
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
                foreach (ISolutionItem iitem in item.Items)
                    InitItem(iitem);
            }
        }
    }
}