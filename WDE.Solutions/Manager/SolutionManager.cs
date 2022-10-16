using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WDE.Common;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    [SingleInstance]
    public class SolutionManager : ISolutionManager
    {
        private readonly IUserSettings userSettings;

        public SolutionManager(IUserSettings userSettings, IParameterFactory parameterFactory)
        {
            this.userSettings = userSettings;
            items = new ObservableList<ISolutionItem>();

            Initialize();

            Items.CollectionChanged += ItemsOnCollectionChanged;
            parameterFactory.OnRegister().SubscribeAction(_ =>
            {
                RefreshAll();
            });
        }

        public event System.Action<ISolutionItem?>? RefreshRequest;
        
        public void Add(ISolutionItem item)
        {
            items.Add(item);
        }

        public void Insert(int index, ISolutionItem item)
        {
            items.Insert(index, item);
        }

        public void Remove(ISolutionItem item)
        {
            items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            items.RemoveAt(index);
        }

        private ObservableList<ISolutionItem> items;
        public IReadOnlyObservableList<ISolutionItem> Items => items;
        public bool CanContainAnyItem => true;

        public void RefreshAll()
        {
            RefreshRequest?.Invoke(null);
        }

        public void Refresh(ISolutionItem item)
        {
            RefreshRequest?.Invoke(item);
            Save();
        }

        public void Initialize()
        {
            var data = userSettings.Get<Data>();
            
            if (data.Items == null)
                return;

            foreach (var item in data.Items)
            {
                InitItem(item);
                items.Add(item);
            }

            RemoveDuplicates(items, new HashSet<ISolutionItem>());
        }

        private void RemoveDuplicates(ObservableCollection<ISolutionItem> items, HashSet<ISolutionItem> added)
        {
            for (var index = items.Count - 1; index >= 0; index--)
            {
                var i = items[index];
                if (i.Items != null)
                    RemoveDuplicates(i.Items, added);
                
                if (!added.Add(items[index]))
                    items.RemoveAt(index);
            }
        }

        private void ItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            IList? newItems = notifyCollectionChangedEventArgs.NewItems;
            IList? oldItems = notifyCollectionChangedEventArgs.OldItems;

            if (newItems != null)
            {
                foreach (ISolutionItem item in newItems)
                {
                    if (item.Items != null)
                        item.Items.CollectionChanged += ItemsOnCollectionChanged;
                }
            }

            if (oldItems != null)
            {
                foreach (ISolutionItem item in oldItems)
                {
                    if (item.Items != null)
                        item.Items.CollectionChanged -= ItemsOnCollectionChanged;
                }
            }

            Save();
        }

        private void Save()
        {
            userSettings.Update<Data>(new Data(items));
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