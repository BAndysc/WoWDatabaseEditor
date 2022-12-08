using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.SmartScriptEditor.Models.Helpers
{
    public class SmartSelectionHelper : System.IDisposable
    {
        private IDisposable disposable;
        private Dictionary<SmartEvent, IDisposable> eventToDisposables = new();
        private Dictionary<SmartAction, IDisposable> actionToDisposables = new();
        private Dictionary<SmartCondition, IDisposable> conditionToDisposables = new();
        public ObservableCollection<object> AllSmartObjectsFlat { get; } = new();
        public ObservableCollection<SmartAction> AllActions { get; } = new();

        public event Action? ScriptSelectedChanged;

        public event Action<SmartEvent?, SmartAction?, EventChangedMask>? EventChanged;
        
        public SmartSelectionHelper(SmartScriptBase script)
        {
            disposable = script.Events.ToStream(false).Subscribe(EventReceived);
        }

        private void EventReceived(CollectionEvent<SmartEvent> collectionEvent)
        {
            EventChanged?.Invoke(collectionEvent.Item, null, EventChangedMask.Event);
            if (collectionEvent.Type == CollectionEventType.Add)
            {
                AllSmartObjectsFlat.Add(collectionEvent.Item);
                var disposables = new CompositeDisposable(
                    collectionEvent.Item.Actions.ToStream(false).Subscribe(ActionReceived),
                    collectionEvent.Item.Conditions.ToStream(false).Subscribe(ConditionReceived),
                    collectionEvent.Item.ToObservable(e => e.IsSelected).Subscribe(SelectionChanged),
                    Observable.FromEventPattern<EventHandler, EventArgs>(
                        h => collectionEvent.Item.OnChanged += h,
                        h => collectionEvent.Item.OnChanged -= h).Subscribe(handler =>
                    {
                        SmartEvent? e = (SmartEvent?) handler.Sender;
                        EventChanged?.Invoke(e, null, EventChangedMask.EventValues);
                    })
                );

                eventToDisposables.Add(collectionEvent.Item, disposables);
            }
            else if (collectionEvent.Type == CollectionEventType.Remove)
            {
                AllSmartObjectsFlat.Remove(collectionEvent.Item);

                eventToDisposables[collectionEvent.Item].Dispose();
                
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
            EventChanged?.Invoke(collectionEvent.Item.Parent, null, EventChangedMask.Actions);
            if (collectionEvent.Type == CollectionEventType.Add)
            {
                AllActions.Add(collectionEvent.Item);
                AllSmartObjectsFlat.Add(collectionEvent.Item);
                actionToDisposables.Add(collectionEvent.Item,
                    new CompositeDisposable(
                    collectionEvent.Item.ToObservable(e => e.IsSelected).Subscribe(SelectionChanged),
                    Observable.FromEventPattern<EventHandler, EventArgs>(
                        h => collectionEvent.Item.OnChanged += h,
                        h => collectionEvent.Item.OnChanged -= h).Subscribe(handler =>
                    {
                        SmartAction? e = (SmartAction?) handler.Sender;
                        EventChanged?.Invoke(e?.Parent, e, EventChangedMask.ActionsValues);
                    })
                ));
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
            EventChanged?.Invoke(collectionEvent.Item.Parent, null, EventChangedMask.Conditions);
            if (collectionEvent.Type == CollectionEventType.Add)
            {
                AllSmartObjectsFlat.Add(collectionEvent.Item);
                conditionToDisposables.Add(collectionEvent.Item,
                    new CompositeDisposable(
                        collectionEvent.Item.ToObservable(e => e.IsSelected).Subscribe(SelectionChanged),
                        Observable.FromEventPattern<EventHandler, EventArgs>(
                            h => collectionEvent.Item.OnChanged += h,
                            h => collectionEvent.Item.OnChanged -= h).Subscribe(handler =>
                        {
                            SmartCondition? e = (SmartCondition?) handler.Sender;
                            EventChanged?.Invoke(e?.Parent, null, EventChangedMask.Conditions);
                        })
                    ));
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
                disposables.Dispose();
            
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
