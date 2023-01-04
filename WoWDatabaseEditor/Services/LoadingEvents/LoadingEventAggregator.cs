using System;
using System.Collections.Generic;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Services.LoadingEvents
{
    public class LoadingEventAggregator : ILoadingEventAggregator
    {
        private HashSet<Type> published = new();
        private Dictionary<Type, OnDemandSingleValuePublisher<ILoadingEvent>> pendings = new();

        public IObservable<ILoadingEvent> OnEvent<T>() where T : ILoadingEvent, new()
        {
            if (published.Contains(typeof(T)))
                return new SingleValuePublisher<ILoadingEvent>(new T());

            if (pendings.TryGetValue(typeof(T), out var pending))
                return pending;

            return pendings[typeof(T)] = new();
        }

        public void Publish<T>() where T : ILoadingEvent, new()
        {
            if (!published.Add(typeof(T)))
                throw new Exception("Can't publish the same loading event twice!");
            if (pendings.TryGetValue(typeof(T), out var pending))
            {
                pending.Publish(new T());
                pendings.Remove(typeof(T));
            }
            published.Add(typeof(T));
        }
    }
}