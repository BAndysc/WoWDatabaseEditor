using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.SmartScriptEditor.Models.Helpers
{
    public class SmartSelectionHelper : System.IDisposable
    {
        private IDisposable disposable;
        private Dictionary<SmartEvent, (IDisposable, IDisposable, IDisposable)> eventToDisposables = new();
        private Dictionary<SmartAction, IDisposable> actionToDisposables = new();
        private Dictionary<SmartCondition, IDisposable> conditionToDisposables = new();
        public ObservableCollection<object> AllSmartObjectsFlat { get; } = new();
        public ObservableCollection<SmartAction> AllActions { get; } = new();

        public event Action ScriptSelectedChanged;
        
        public SmartSelectionHelper(SmartScript script)
        {
            disposable = script.Events.ToStream().Subscribe(EventReceived);
        }

        private void EventReceived(CollectionEvent<SmartEvent> collectionEvent)
        {
            if (collectionEvent.Type == CollectionEventType.Add)
            {
                AllSmartObjectsFlat.Add(collectionEvent.Item);
                var disposables = (
                    collectionEvent.Item.Actions.ToStream().Subscribe(ActionReceived),
                    collectionEvent.Item.Conditions.ToStream().Subscribe(ConditionReceived),
                    collectionEvent.Item.ToObservable(e => e.IsSelected).Subscribe(SelectionChanged)
                );

                eventToDisposables.Add(collectionEvent.Item, disposables);
            }
            else if (collectionEvent.Type == CollectionEventType.Remove)
            {
                AllSmartObjectsFlat.Remove(collectionEvent.Item);

                var disposables = eventToDisposables[collectionEvent.Item];
                disposables.Item1.Dispose();
                disposables.Item2.Dispose();
                disposables.Item3.Dispose();

                foreach (var action in collectionEvent.Item.Actions)
                {
                    AllSmartObjectsFlat.Remove(action);
                    actionToDisposables[action].Dispose();
                    actionToDisposables.Remove(action);
                }
                
                foreach (var condition in collectionEvent.Item.Conditions)
                {
                    AllSmartObjectsFlat.Remove(condition);
                    conditionToDisposables[condition].Dispose();
                    conditionToDisposables.Remove(condition);
                }
                eventToDisposables.Remove(collectionEvent.Item);
            }
        }

        private void ActionReceived(CollectionEvent<SmartAction> collectionEvent)
        {
            if (collectionEvent.Type == CollectionEventType.Add)
            {
                AllActions.Add(collectionEvent.Item);
                AllSmartObjectsFlat.Add(collectionEvent.Item);
                actionToDisposables.Add(collectionEvent.Item,
                    collectionEvent.Item.ToObservable(e => e.IsSelected).Subscribe(SelectionChanged));
            }
            else if (collectionEvent.Type == CollectionEventType.Remove)
            {
                AllActions.Remove(collectionEvent.Item);
                AllSmartObjectsFlat.Remove(collectionEvent.Item);
                actionToDisposables[collectionEvent.Item].Dispose();
                actionToDisposables.Remove(collectionEvent.Item);
            }
        }
        
        private void ConditionReceived(CollectionEvent<SmartCondition> collectionEvent)
        {
            if (collectionEvent.Type == CollectionEventType.Add)
            {
                AllSmartObjectsFlat.Add(collectionEvent.Item);
                conditionToDisposables.Add(collectionEvent.Item,
                    collectionEvent.Item.ToObservable(e => e.IsSelected).Subscribe(SelectionChanged));
            }
            else if (collectionEvent.Type == CollectionEventType.Remove)
            {
                AllSmartObjectsFlat.Remove(collectionEvent.Item);
                conditionToDisposables[collectionEvent.Item].Dispose();
                conditionToDisposables.Remove(collectionEvent.Item);
            }
        }

        private void SelectionChanged(bool selected)
        {
            ScriptSelectedChanged?.Invoke();
        }

        public void Dispose()
        {
            disposable.Dispose();
            
            foreach (var disposables in eventToDisposables.Values)
            {
                disposables.Item1.Dispose();
                disposables.Item2.Dispose();
                disposables.Item3.Dispose();
            }
            
            foreach (var d in actionToDisposables.Values)
                d.Dispose();
            
            foreach (var d in conditionToDisposables.Values)
                d.Dispose();
            
            eventToDisposables.Clear();
            actionToDisposables.Clear();
            conditionToDisposables.Clear();
        }
    }
}