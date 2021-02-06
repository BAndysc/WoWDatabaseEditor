using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WDE.Common.History;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.ViewModels;

namespace WDE.SmartScriptEditor.History
{
    public class SmartDataGroupsHistory: HistoryHandler, IDisposable
    {
        private readonly ObservableCollection<SmartDataGroupsEditorData> groupsData;
        private readonly IDisposable groupsDataSubscription;

        public SmartDataGroupsHistory(ObservableCollection<SmartDataGroupsEditorData> groupsData)
        {
            this.groupsData = groupsData;
            groupsDataSubscription = groupsData.ToStream().Subscribe(e =>
            {
                if (e.Type == CollectionEventType.Add)
                {
                    PushAction(new SmartDataGroupHistoryAction(e.Item, e.Index,
                        GroupHistoryActionMode.ActionAdd, groupsData));
                    BindGroupData(e.Item);
                }
                else if (e.Type == CollectionEventType.Remove)
                {
                    UnbindGroupData(e.Item);
                    PushAction(new SmartDataGroupHistoryAction(e.Item, e.Index,
                        GroupHistoryActionMode.ActionRemove, groupsData));
                }
            });
            foreach (var data in groupsData)
                BindGroupData(data);
        }
        
        public void Dispose()
        {
            groupsDataSubscription.Dispose();
            foreach (var data in groupsData)
                UnbindGroupData(data);
        }

        private void OnDataNameChanged(SmartDataGroupsEditorData data, string newName, string oldName)
        {
            PushAction(new SmartDataGroupNameHistoryModifedAction(data, oldName, newName));
        }

        private void OnGroupMembersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        var node = item as SmartDataGroupsEditorDataNode;
                        PushAction(new SmartDataGroupMemberHistoryAction(node, e.NewStartingIndex,
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
                        var node = item as SmartDataGroupsEditorDataNode;
                        PushAction(new SmartDataGroupMemberHistoryAction(node, e.OldStartingIndex,
                            GroupHistoryActionMode.ActionRemove, node.Owner, node.Owner.Members));
                    }
                }
            }
        }

        private void BindGroupData(SmartDataGroupsEditorData data)
        {
            data.OnNameChanged += OnDataNameChanged;
            data.Members.CollectionChanged += OnGroupMembersCollectionChanged;
        }

        private void UnbindGroupData(SmartDataGroupsEditorData data)
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
    
    internal class SmartDataGroupHistoryAction : IHistoryAction
    {
        private readonly SmartDataGroupsEditorData element;
        private readonly int index;
        private readonly GroupHistoryActionMode actionMode;
        private readonly ObservableCollection<SmartDataGroupsEditorData> collection;

        public SmartDataGroupHistoryAction(SmartDataGroupsEditorData element, int index, GroupHistoryActionMode actionMode,
            ObservableCollection<SmartDataGroupsEditorData> collection)
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

    internal class SmartDataGroupNameHistoryModifedAction: IHistoryAction
    {
        private readonly SmartDataGroupsEditorData element;
        private readonly string oldName;
        private readonly string newName;

        public SmartDataGroupNameHistoryModifedAction(SmartDataGroupsEditorData element, string oldName, string newName)
        {
            this.element = element;
            this.oldName = oldName;
            this.newName = newName;
        }

        public void Undo() => element.Name = oldName;

        public void Redo() => element.Name = newName;

        public string GetDescription() => $"Modified name of group {oldName}";
    }
    
    internal class SmartDataGroupMemberHistoryAction : IHistoryAction
    {
        private readonly SmartDataGroupsEditorDataNode element;
        private readonly int index;
        private readonly GroupHistoryActionMode actionMode;
        private readonly SmartDataGroupsEditorData elementOwner;
        private readonly ObservableCollection<SmartDataGroupsEditorDataNode> collection;

        public SmartDataGroupMemberHistoryAction(SmartDataGroupsEditorDataNode element, int index, GroupHistoryActionMode actionMode,
            SmartDataGroupsEditorData elementOwner, ObservableCollection<SmartDataGroupsEditorDataNode> collection)
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