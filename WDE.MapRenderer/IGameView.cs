using System;
using System.Collections.Generic;
using WDE.MapRenderer.Managers;
using WDE.Module.Attributes;

namespace WDE.MapRenderer
{
    [UniqueProvider]
    public interface IGameView
    {
        public IEnumerable<Func<IGameModule>> Modules { get; }
        public event Action<Func<IGameModule>> ModuleRegistered;
        public event Action<Func<IGameModule>> ModuleRemoved; 
        public System.IDisposable RegisterGameModule(Func<IGameModule> gameModule);

        public Task<IGameContext> Open();
    }

    public interface IGameModule : System.IDisposable
    {
        void Initialize(IGameContext gameContext);
        void Update(float delta);
        void Render();
    }
}