using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using Newtonsoft.Json;
using Prism.Events;
using WDE.Common;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    [SingleInstance]
    public class SolutionManager : ISolutionManager
    {
        private readonly IUserSettings userSettings;

        public SolutionManager(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            Items = new ObservableCollection<ISolutionItem>();

            Initialize();

            Items.CollectionChanged += ItemsOnCollectionChanged;
        }

        public ObservableCollection<ISolutionItem> Items { get; }

        public void Initialize()
        {
            var data = userSettings.Get<Data>();
            
            if (data.Items == null)
                return;

            foreach (var item in data.Items)
            {
                InitItem(item);
                Items.Add(item);
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
            userSettings.Update<Data>(new Data(Items));
        }

        private void InitItem(ISolutionItem item)
        {
            if (item.Items != null)
                item.Items.CollectionChanged += ItemsOnCollectionChanged;

            if (item.Items != null)
            {
                foreach (ISolutionItem iitem in item.Items)
                    InitItem(iitem);
            }
        }

        private struct Data : ISettings
        {
            public Data(IList<ISolutionItem> items)
            {
                Items = items;
            }

            public IList<ISolutionItem> Items { get; set; }
        }
    }
}