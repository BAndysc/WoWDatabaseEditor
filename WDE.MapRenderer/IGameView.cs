using System.Collections;
using System.Windows.Input;
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
        object? ToolBar => null;
        void Initialize();
        void Update(float delta);
        void Render(float delta) { }
        void RenderTransparent() { }
        void RenderGUI() { }
        IEnumerator LoadChunk(int mapId, int chunkX, int chunkZ, CancellationToken cancellationToken)
        {
            yield break;
        }
        IEnumerator UnloadChunk(int chunkX, int chunkZ)
        {
            yield break;
        }

        IEnumerable<(string, ICommand, object?)>? GenerateContextMenu() => null;
    }
}