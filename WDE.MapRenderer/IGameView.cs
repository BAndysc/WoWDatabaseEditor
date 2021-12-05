using Prism.Ioc;
using WDE.Module.Attributes;

namespace WDE.MapRenderer
{
    [UniqueProvider]
    public interface IGameView
    {
        public IEnumerable<Func<IContainerProvider,IGameModule>> Modules { get; }
        public event Action<Func<IContainerProvider,IGameModule>> ModuleRegistered;
        public event Action<Func<IContainerProvider,IGameModule>> ModuleRemoved; 
        public System.IDisposable RegisterGameModule(Func<IContainerProvider, IGameModule> gameModule);
        public Task<Game> Open();
    }

    public interface IGameModule : System.IDisposable
    {
        object? ViewModel { get; }
        void Initialize();
        void Update(float delta);
        void Render();
        void RenderGUI();
    }
}