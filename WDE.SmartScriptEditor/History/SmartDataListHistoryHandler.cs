using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WDE.Common.History;
using WDE.SmartScriptEditor.Data;

namespace WDE.TrinitySmartScriptEditor.History
{
    public class SmartDataListHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly ObservableCollection<SmartGenericJsonData> smartDataList;

        public SmartDataListHistoryHandler(ObservableCollection<SmartGenericJsonData> smartDataList)
        {
            this.smartDataList = smartDataList;
            smartDataList.CollectionChanged += OnDataCollectionChanged;
        }

        public void Dispose()
        {
            smartDataList.CollectionChanged -= OnDataCollectionChanged;
        }

        private void OnDataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (SmartGenericJsonData data in e.NewItems)
                        PushAction(new SmartDataListHistoryAction(in data, e.NewStartingIndex, ListHistoryActionMode.ActionAdd, smartDataList));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach(SmartGenericJsonData data in e.OldItems)
                        PushAction(new SmartDataListHistoryAction(in data, e.OldStartingIndex, ListHistoryActionMode.ActionRemove, smartDataList));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (e.NewItems != null && e.OldItems != null && e.NewItems.Count == e.OldItems.Count)
                {
                    for (int i = 0; i < e.NewItems.Count; ++i)
                    {
                        SmartGenericJsonData oldItem = e.OldItems[i] is SmartGenericJsonData ? (SmartGenericJsonData) e.OldItems[i] : default;
                        SmartGenericJsonData newItem = e.NewItems[i] is SmartGenericJsonData ? (SmartGenericJsonData) e.NewItems[i] : default;
                        PushAction(new SmartDataListHistoryReplaceAction(in oldItem, in newItem, e.NewStartingIndex, smartDataList));
                    }
                }
            }
        }
    }

    internal enum ListHistoryActionMode
    {
        ActionAdd,
        ActionRemove,
    }
    
    internal class SmartDataListHistoryAction : IHistoryAction
    {
        private readonly SmartGenericJsonData element;
        private readonly int index;
        private readonly ListHistoryActionMode actionMode;
        private readonly ObservableCollection<SmartGenericJsonData> collection;

        internal SmartDataListHistoryAction(in SmartGenericJsonData element, int index, ListHistoryActionMode actionMode, ObservableCollection<SmartGenericJsonData> collection)
        {
            this.element = element;
            this.index = index;
            this.actionMode = actionMode;
            this.collection = collection;
        } 
        
        public void Undo()
        {
            if (actionMode == ListHistoryActionMode.ActionAdd)
               Change(true);
            else
                Change(false);
        }

        public void Redo()
        {
            if (actionMode == ListHistoryActionMode.ActionAdd)
                Change(false);
            else
                Change(true);
        }

        private void Change(bool remove)
        {
            if (remove)
                collection.Remove(element);
            else
                collection.Insert(index, element);
        }

        public string GetDescription()
        {
            return $"{(actionMode == ListHistoryActionMode.ActionAdd ? "Added " : "Removed")} {element.Name}";
        }
    }

    internal class SmartDataListHistoryReplaceAction : IHistoryAction
    {
        private readonly SmartGenericJsonData old;
        private readonly SmartGenericJsonData @new;
        private readonly int index;
        private readonly ObservableCollection<SmartGenericJsonData> collection;

        internal SmartDataListHistoryReplaceAction(in SmartGenericJsonData old, in SmartGenericJsonData @new, int index,
            ObservableCollection<SmartGenericJsonData> collection)
        {
            this.old = old;
            this.@new = @new;
            this.index = index;
            this.collection = collection;
        }
        
        public void Undo()
        {
            collection[index] = old;
        }

        public void Redo()
        {
            collection[index] = @new;
        }

        public string GetDescription()
        {
            return $"Modified {old.Name}";
        }
    }
}