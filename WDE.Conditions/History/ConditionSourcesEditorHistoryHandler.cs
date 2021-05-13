using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WDE.Common.History;
using WDE.Conditions.Data;

namespace WDE.Conditions.History
{
    public class ConditionSourcesEditorHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly ObservableCollection<ConditionSourcesJsonData> sourcesList;

        public ConditionSourcesEditorHistoryHandler(ObservableCollection<ConditionSourcesJsonData> sourcesList)
        {
            this.sourcesList = sourcesList;
            sourcesList.CollectionChanged += OnDataCollectionChanged;
        }

        public void Dispose()
        {
            sourcesList.CollectionChanged -= OnDataCollectionChanged;
        }

        private void OnDataCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (ConditionSourcesJsonData data in e.NewItems)
                        PushAction(new CondtionSourcesListHistoryAction(in data, e.NewStartingIndex, SourcesListHistoryActionMode.ActionAdd, sourcesList));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach(ConditionSourcesJsonData data in e.OldItems)
                        PushAction(new CondtionSourcesListHistoryAction(in data, e.OldStartingIndex, SourcesListHistoryActionMode.ActionRemove, sourcesList));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (e.NewItems != null && e.OldItems != null && e.NewItems.Count == e.OldItems.Count)
                {
                    for (int i = 0; i < e.NewItems.Count; ++i)
                    {
                        ConditionSourcesJsonData oldItem = e.OldItems[i] is ConditionSourcesJsonData ? (ConditionSourcesJsonData) e.OldItems[i]! : default;
                        ConditionSourcesJsonData newItem = e.NewItems[i] is ConditionSourcesJsonData ? (ConditionSourcesJsonData) e.NewItems[i]! : default;
                        PushAction(new ConditionSourcesListHistoryReplaceAction(in oldItem, in newItem, e.NewStartingIndex, sourcesList));
                    }
                }
            }
        }
    }

    internal enum SourcesListHistoryActionMode
    {
        ActionAdd,
        ActionRemove,
    }
    
    internal class CondtionSourcesListHistoryAction : IHistoryAction
    {
        private readonly ConditionSourcesJsonData element;
        private readonly int index;
        private readonly SourcesListHistoryActionMode actionMode;
        private readonly ObservableCollection<ConditionSourcesJsonData> collection;

        internal CondtionSourcesListHistoryAction(in ConditionSourcesJsonData element, int index, SourcesListHistoryActionMode actionMode, ObservableCollection<ConditionSourcesJsonData> collection)
        {
            this.element = element;
            this.index = index;
            this.actionMode = actionMode;
            this.collection = collection;
        } 
        
        public void Undo()
        {
            if (actionMode == SourcesListHistoryActionMode.ActionAdd)
               Change(true);
            else
                Change(false);
        }

        public void Redo()
        {
            if (actionMode == SourcesListHistoryActionMode.ActionAdd)
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
            return $"{(actionMode == SourcesListHistoryActionMode.ActionAdd ? "Added " : "Removed")} {element.Name}";
        }
    }

    internal class ConditionSourcesListHistoryReplaceAction : IHistoryAction
    {
        private readonly ConditionSourcesJsonData old;
        private readonly ConditionSourcesJsonData @new;
        private readonly int index;
        private readonly ObservableCollection<ConditionSourcesJsonData> collection;

        internal ConditionSourcesListHistoryReplaceAction(in ConditionSourcesJsonData old, in ConditionSourcesJsonData @new, int index,
            ObservableCollection<ConditionSourcesJsonData> collection)
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