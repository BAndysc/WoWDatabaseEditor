using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WDE.Common.History;
using WDE.Conditions.Data;

namespace WDE.Conditions.History
{
    public class ConditionsDefinitionEditorHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly ObservableCollection<ConditionJsonData> conditionsList;

        public ConditionsDefinitionEditorHistoryHandler(ObservableCollection<ConditionJsonData> conditionsList)
        {
            this.conditionsList = conditionsList;
            conditionsList.CollectionChanged += OnDataCollectionChanged;
        }

        public void Dispose()
        {
            conditionsList.CollectionChanged -= OnDataCollectionChanged;
        }

        private void OnDataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (ConditionJsonData data in e.NewItems)
                        PushAction(new ConditionsListHistoryAction(in data, e.NewStartingIndex, ListHistoryActionMode.ActionAdd, conditionsList));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach(ConditionJsonData data in e.OldItems)
                        PushAction(new ConditionsListHistoryAction(in data, e.OldStartingIndex, ListHistoryActionMode.ActionRemove, conditionsList));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (e.NewItems != null && e.OldItems != null && e.NewItems.Count == e.OldItems.Count)
                {
                    for (int i = 0; i < e.NewItems.Count; ++i)
                    {
                        ConditionJsonData oldItem = e.OldItems[i] is ConditionJsonData ? (ConditionJsonData) e.OldItems[i] : default;
                        ConditionJsonData newItem = e.NewItems[i] is ConditionJsonData ? (ConditionJsonData) e.NewItems[i] : default;
                        PushAction(new CondtionsListHistoryReplaceAction(in oldItem, in newItem, e.NewStartingIndex, conditionsList));
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
    
    internal class ConditionsListHistoryAction : IHistoryAction
    {
        private readonly ConditionJsonData element;
        private readonly int index;
        private readonly ListHistoryActionMode actionMode;
        private readonly ObservableCollection<ConditionJsonData> collection;

        internal ConditionsListHistoryAction(in ConditionJsonData element, int index, ListHistoryActionMode actionMode, ObservableCollection<ConditionJsonData> collection)
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

    internal class CondtionsListHistoryReplaceAction : IHistoryAction
    {
        private readonly ConditionJsonData old;
        private readonly ConditionJsonData @new;
        private readonly int index;
        private readonly ObservableCollection<ConditionJsonData> collection;

        internal CondtionsListHistoryReplaceAction(in ConditionJsonData old, in ConditionJsonData @new, int index,
            ObservableCollection<ConditionJsonData> collection)
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