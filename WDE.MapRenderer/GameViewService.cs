using Prism.Ioc;
using WDE.Common.Disposables;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.MapRenderer
{
    [AutoRegister]
    [SingleInstance]
    public class GameViewService : IGameView
    {
        private readonly Lazy<IDocumentManager> documentManager;
        private List<Func<IContainerProvider, IGameModule>> modules = new();

        public IEnumerable<Func<IContainerProvider, IGameModule>> Modules => modules;
        public event Action<Func<IContainerProvider, IGameModule>>? ModuleRegistered;
        public event Action<Func<IContainerProvider, IGameModule>>? ModuleRemoved;

        public GameViewService(Lazy<IDocumentManager> documentManager)
        {
            this.documentManager = documentManager;
        }
        
        public IDisposable RegisterGameModule(Func<IContainerProvider, IGameModule> gameModule)
        {
            modules.Add(gameModule);
            ModuleRegistered?.Invoke(gameModule);
            return new ActionDisposable(() =>
            {
                var indexOf = modules.IndexOf(gameModule);
                ModuleRemoved?.Invoke(gameModule);
                modules[indexOf] = modules[^1];
                modules.RemoveAt(modules.Count - 1);
            });
        }

        public async Task<Game> Open()
        {
            documentManager.Value.OpenTool<GameViewModel>();
            var tool = documentManager.Value.GetTool<GameViewModel>();
            return tool.CurrentGame;
        }
    }
}