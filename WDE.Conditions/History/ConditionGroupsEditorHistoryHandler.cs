using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WDE.Common.History;
using WDE.Conditions.ViewModels;
using WDE.MVVM.Observable;

namespace WDE.Conditions.History
{
    public class ConditionGroupsEditorHistoryHandler: HistoryHandler, IDisposable
    {
        private readonly ObservableCollection<ConditionGroupsEditorData> groupsData;
        private readonly IDisposable groupsDataSubscription;

        public ConditionGroupsEditorHistoryHandler(ObservableCollection<ConditionGroupsEditorData> groupsData)
        {
            this.groupsData = groupsData;
            groupsDataSubscription = groupsData.ToStream().Subscribe(e =>
            {
                if (e.Type == CollectionEventType.Add)
                {
                    PushAction(new ConditionGroupHistoryAction(e.Item, e.Index,
                        GroupHistoryActionMode.ActionAdd, groupsData));
                    BindGroupData(e.Item);
                }
                else if (e.Type == CollectionEventType.Remove)
                {
                    UnbindGroupData(e.Item);
                    PushAction(new ConditionGroupHistoryAction(e.Item, e.Index,
                        GroupHistoryActionMode.ActionRemove, groupsData));
                }
            });
        }
        
        public void Dispose()
        {
            groupsDataSubscription.Dispose();
            foreach (var data in groupsData)
                UnbindGroupData(data);
        }

        private void OnDataNameChanged(ConditionGroupsEditorData data, string newName, string oldName)
        {
            PushAction(new ConditionGroupNameHistoryModifedAction(data, oldName, newName));
        }

        private void OnGroupMembersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        var node = item as ConditionGroupsEditorDataNode;
                        PushAction(new ConditionGroupMemberHistoryAction(node, e.NewStartingIndex,
                            GroupHistoryActionMode.ActionAdd, node.Owner, node.Owner.Members));
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        var node = item as ConditionGroupsEditorDataNode;
                        PushAction(new ConditionGroupMemberHistoryAction(node, e.OldStartingIndex,
                            GroupHistoryActionMode.ActionRemove, node.Owner, node.Owner.Members));
                    }
                }
            }
        }

        private void BindGroupData(ConditionGroupsEditorData data)
        {
            data.OnNameChanged += OnDataNameChanged;
            data.Members.CollectionChanged += OnGroupMembersCollectionChanged;
        }

        private void UnbindGroupData(ConditionGroupsEditorData data)
        {
            data.OnNameChanged -= OnDataNameChanged;
            data.Members.CollectionChanged -= OnGroupMembersCollectionChanged;
        }
    }

    internal enum GroupHistoryActionMode 
    {
        ActionAdd,
        ActionRemove,
    }
    
    internal class ConditionGroupHistoryAction : IHistoryAction
    {
        private readonly ConditionGroupsEditorData element;
        private readonly int index;
        private readonly GroupHistoryActionMode actionMode;
        private readonly ObservableCollection<ConditionGroupsEditorData> collection;

        public ConditionGroupHistoryAction(ConditionGroupsEditorData element, int index, GroupHistoryActionMode actionMode,
            ObservableCollection<ConditionGroupsEditorData> collection)
        {
            this.element = element;
            this.index = index;
            this.actionMode = actionMode;
            this.collection = collection;
        }

        public void Undo()
        {
            if (actionMode == GroupHistoryActionMode.ActionAdd)
                Change(true);
            else
                Change(false);
        }

        public void Redo()
        {
            if (actionMode == GroupHistoryActionMode.ActionAdd)
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
            return $"{(actionMode == GroupHistoryActionMode.ActionAdd ? "Added to" : "Removed from")} group {element.Name}";
        }
    }

    internal class ConditionGroupNameHistoryModifedAction: IHistoryAction
    {
        private readonly ConditionGroupsEditorData element;
        private readonly string oldName;
        private readonly string newName;

        public ConditionGroupNameHistoryModifedAction(ConditionGroupsEditorData element, string oldName, string newName)
        {
            this.element = element;
            this.oldName = oldName;
            this.newName = newName;
        }

        public void Undo() => element.Name = oldName;

        public void Redo() => element.Name = newName;

        public string GetDescription() => $"Modified name of group {oldName}";
    }
    
    internal class ConditionGroupMemberHistoryAction : IHistoryAction
    {
        private readonly ConditionGroupsEditorDataNode element;
        private readonly int index;
        private readonly GroupHistoryActionMode actionMode;
        private readonly ConditionGroupsEditorData elementOwner;
        private readonly ObservableCollection<ConditionGroupsEditorDataNode> collection;

        public ConditionGroupMemberHistoryAction(ConditionGroupsEditorDataNode element, int index, GroupHistoryActionMode actionMode,
            ConditionGroupsEditorData elementOwner, ObservableCollection<ConditionGroupsEditorDataNode> collection)
        {
            this.element = element;
            this.index = index;
            this.actionMode = actionMode;
            this.elementOwner = elementOwner;
            this.collection = collection;
        }

        public void Undo()
        {
            if (actionMode == GroupHistoryActionMode.ActionAdd)
                Change(true);
            else
                Change(false);
        }

        public void Redo()
        {
            if (actionMode == GroupHistoryActionMode.ActionAdd)
                Change(false);
            else
                Change(true);
        }

        private void Change(bool remove)
        {
            if (remove)
            {
                collection.Remove(element);
                element.Owner = null;
            }
            else
            {
                element.Owner = elementOwner;
                collection.Insert(index, element);
            }
        }

        public string GetDescription()
        {
            bool isAdd = actionMode == GroupHistoryActionMode.ActionAdd;
            return $"{(isAdd ? "Added" : "Removed")} {element.Name} {(isAdd ? "to" : "from")} group {elementOwner.Name}";
        }
    }
}