using System.Collections.ObjectModel;
using Prism.Ioc;
using WDE.Common.Utils;
using WDE.MVVM.Observable;

namespace WDE.MapRenderer.Managers
{
    public class ModuleManager : System.IDisposable
    {
        private readonly IContainerProvider containerProvider;
        private readonly IGameView gameView;

        private HashSet<Func<IContainerProvider, IGameModule>> modulesToAdd = new();
        private HashSet<Func<IContainerProvider, IGameModule>> modulesToRemove = new();
        private List<(Func<IContainerProvider, IGameModule>, IGameModule, object?)> modules = new();
        public ObservableCollection<object> ViewModels { get; } = new();

        public ModuleManager(IContainerProvider containerProvider,
            IGameView gameView)
        {
            this.containerProvider = containerProvider;
            this.gameView = gameView;
            
            foreach (var m in gameView.Modules)
                modulesToAdd.Add(m);

            gameView.ModuleRegistered += GameViewOnModuleRegistered;
            gameView.ModuleRemoved += GameViewOnModuleRemoved;
        }

        public void Update(float delta)
        {
            foreach (var m in modulesToAdd)
            {
                var moduleInstance = m(containerProvider);
                moduleInstance.Initialize();
                modules.Add((m, moduleInstance, moduleInstance.ViewModel));
                if (moduleInstance.ViewModel != null)
                    ViewModels.Add(moduleInstance.ViewModel);
            }
            modulesToAdd.Clear();
            foreach (var toRemove in modulesToRemove)
            {
                for (int i = 0; i < modules.Count; ++i)
                {
                    if (modules[i].Item1 == toRemove)
                    {
                        modules[i].Item2.Dispose();
                        if (modules[i].Item3 != null)
                            ViewModels.Remove(modules[i].Item3);
                        modules.RemoveAt(i);
                        break;
                    }
                }
            }
            modulesToRemove.Clear();
            foreach (var module in modules)
                module.Item2.Update(delta);
        }

        private void GameViewOnModuleRemoved(Func<IContainerProvider, IGameModule> m)
        {
            modulesToRemove.Add(m);
        }

        private void GameViewOnModuleRegistered(Func<IContainerProvider, IGameModule> m)
        {
            modulesToAdd.Add(m);
        }

        public void Dispose()
        {
            gameView.ModuleRegistered -= GameViewOnModuleRegistered;
            gameView.ModuleRemoved -= GameViewOnModuleRemoved;
            foreach (var module in modules)
                module.Item2.Dispose();
            ViewModels.RemoveAll();
            modules.Clear();
        }

        public void Render()
        {
            foreach (var module in modules)
                module.Item2.Render();
        }

        public void RenderGUI()
        {
            foreach (var module in modules)
                module.Item2.RenderGUI();
        }
    }
}