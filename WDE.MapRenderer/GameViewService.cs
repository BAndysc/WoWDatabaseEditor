using Prism.Ioc;
using WDE.Common.Disposables;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.MapRenderer
{
    [AutoRegister]
    [SingleInstance]
    public class GameViewService : IGameView, IGameViewService
    {
        private readonly Lazy<IDocumentManager> documentManager;
        private readonly IContainerProvider provider;
        private List<Func<IContainerProvider, IGameModule>> modules = new();

        public IEnumerable<Func<IContainerProvider, IGameModule>> Modules => modules;
        public event Action<Func<IContainerProvider, IGameModule>>? ModuleRegistered;
        public event Action<Func<IContainerProvider, IGameModule>>? ModuleRemoved;

        public GameViewService(Lazy<IDocumentManager> documentManager, IContainerProvider provider)
        {
            this.documentManager = documentManager;
            this.provider = provider;
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
            var existing = documentManager.Value.TryFindDocumentEditor<GameViewModel>(x => true);
            if (existing == null)
            {
                existing = provider.Resolve<GameViewModel>();
                documentManager.Value.OpenDocument(existing);
            }
            else
                documentManager.Value.ActiveDocument = existing;
            return existing.CurrentGame;
        }

        void IGameViewService.Open()
        {
            Open().ListenErrors();
        }
    }
}