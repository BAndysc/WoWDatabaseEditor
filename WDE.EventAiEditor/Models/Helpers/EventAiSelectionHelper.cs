using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.EventAiEditor.Models.Helpers
{
    public class EventAiSelectionHelper : System.IDisposable
    {
        private IDisposable disposable;
        private Dictionary<EventAiEvent, IDisposable> eventToDisposables = new();
        private Dictionary<EventAiAction, IDisposable> actionToDisposables = new();
        public ObservableCollection<object> AllObjectsFlat { get; } = new();
        public ObservableCollection<EventAiAction> AllActions { get; } = new();

        public event Action? ScriptSelectedChanged;

        public event Action<EventAiEvent?, EventAiAction?, EventChangedMask>? EventChanged;
        
        public EventAiSelectionHelper(EventAiBase script)
        {
            disposable = script.Events.ToStream(false).Subscribe(EventReceived);
        }

        private void EventReceived(CollectionEvent<EventAiEvent> collectionEvent)
        {
            EventChanged?.Invoke(collectionEvent.Item, null, EventChangedMask.Event);
            if (collectionEvent.Type == CollectionEventType.Add)
            {
                AllObjectsFlat.Add(collectionEvent.Item);
                var disposables = new CompositeDisposable(
                    collectionEvent.Item.Actions.ToStream(false).Subscribe(ActionReceived),
                    collectionEvent.Item.ToObservable(e => e.IsSelected).Subscribe(SelectionChanged),
                    Observable.FromEventPattern<EventHandler, EventArgs>(
                        h => collectionEvent.Item.OnChanged += h,
                        h => collectionEvent.Item.OnChanged -= h).Subscribe(handler =>
                    {
                        EventAiEvent? e = (EventAiEvent?) handler.Sender;
                        EventChanged?.Invoke(e, null, EventChangedMask.EventValues);
                    })
                );

                eventToDisposables.Add(collectionEvent.Item, disposables);
            }
            else if (collectionEvent.Type == CollectionEventType.Remove)
            {
                AllObjectsFlat.Remove(collectionEvent.Item);

                eventToDisposables[collectionEvent.Item].Dispose();
                
                foreach (var action in collectionEvent.Item.Actions)
                {
                    AllObjectsFlat.Remove(action);
                    actionToDisposables[action].Dispose();
                    actionToDisposables.Remove(action);
                }
                
                eventToDisposables.Remove(collectionEvent.Item);
            }
        }

        private void ActionReceived(CollectionEvent<EventAiAction> collectionEvent)
        {
            EventChanged?.Invoke(collectionEvent.Item.Parent, null, EventChangedMask.Actions);
            if (collectionEvent.Type == CollectionEventType.Add)
            {
                AllActions.Add(collectionEvent.Item);
                AllObjectsFlat.Add(collectionEvent.Item);
                actionToDisposables.Add(collectionEvent.Item,
                    new CompositeDisposable(
                    collectionEvent.Item.ToObservable(e => e.IsSelected).Subscribe(SelectionChanged),
                    Observable.FromEventPattern<EventHandler, EventArgs>(
                        h => collectionEvent.Item.OnChanged += h,
                        h => collectionEvent.Item.OnChanged -= h).Subscribe(handler =>
                    {
                        EventAiAction? e = (EventAiAction?) handler.Sender;
                        EventChanged?.Invoke(e?.Parent, e, EventChangedMask.ActionsValues);
                    })
                ));
            }
            else if (collectionEvent.Type == CollectionEventType.Remove)
            {
                AllActions.Remove(collectionEvent.Item);
                AllObjectsFlat.Remove(collectionEvent.Item);
                actionToDisposables[collectionEvent.Item].Dispose();
                actionToDisposables.Remove(collectionEvent.Item);
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
            
            eventToDisposables.Clear();
            actionToDisposables.Clear();
        }
    }
}
