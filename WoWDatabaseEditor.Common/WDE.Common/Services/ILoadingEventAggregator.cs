using System;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface ILoadingEventAggregator
    {
        IObservable<ILoadingEvent> OnEvent<T>() where T : ILoadingEvent, new();
        void Publish<T>() where T : ILoadingEvent, new();
    }

    public interface ILoadingEvent
    {
    }

    public struct EditorLoaded : ILoadingEvent
    {
    }
}