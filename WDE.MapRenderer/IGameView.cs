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

    /// <summary>
    /// Contributes type registrations to each newly created per-game scoped container
    /// (<see cref="Game.Initialize"/> invokes all registrars right after its own registrations).
    /// Implementations live in the GLOBAL container ([AutoRegister]) and must be stateless: this is
    /// the place for 3D-only services which must NOT be [AutoRegister] themselves, because a global
    /// registration would keep them (and everything they reference) alive after the game view closes.
    /// The game scope's default lifetime makes every registration a singleton within one game
    /// session, disposed together with the scope.
    /// </summary>
    [NonUniqueProvider]
    public interface IGameScopeRegistrar
    {
        void RegisterScopedTypes(IContainerRegistry gameScope);
    }

    public interface IGameModule : System.IDisposable
    {
        object? ViewModel { get; }
        void Initialize();
        void Update(float delta);
        void Render(float delta) { }
        void RenderTransparent() { }
        void RenderGUI() { }
        ValueTask LoadChunk(int mapId, int chunkX, int chunkZ, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
        ValueTask UnloadChunk(int chunkX, int chunkZ)
        {
            return ValueTask.CompletedTask;
        }

        IEnumerable<(string, ICommand, object?)>? GenerateContextMenu() => null;
    }
}