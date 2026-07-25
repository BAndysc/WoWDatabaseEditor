using System.Collections;
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

        // registration comes from the UI thread (IGameView.RegisterGameModule), draining happens
        // on the game thread in Update - hence the lock
        private readonly object pendingModulesLock = new();
        private HashSet<Func<IContainerProvider, IGameModule>> modulesToAdd = new();
        private HashSet<Func<IContainerProvider, IGameModule>> modulesToRemove = new();
        private List<(Func<IContainerProvider, IGameModule>, IGameModule, object?)> modules = new();
        // immutable snapshot for ForEach, which the UI thread calls too (context menu generation)
        private volatile IGameModule[] modulesSnapshot = [];
        public ObservableCollection<object> ViewModels { get; } = new();

        public ModuleManager(IContainerProvider containerProvider,
            IGameView gameView)
        {
            this.containerProvider = containerProvider;
            this.gameView = gameView;

            lock (pendingModulesLock)
            {
                foreach (var m in gameView.Modules)
                    modulesToAdd.Add(m);
            }

            gameView.ModuleRegistered += GameViewOnModuleRegistered;
            gameView.ModuleRemoved += GameViewOnModuleRemoved;
        }

        public void ForEach(Action<IGameModule> action)
        {
            foreach (var mod in modulesSnapshot)
                action(mod);
        }

        public async ValueTask ForEach(Func<IGameModule, ValueTask> action)
        {
            var snapshot = modulesSnapshot;
            for (var index = 0; index < snapshot.Length; index++)
            {
                await action(snapshot[index]);
            }
        }

        public void Update(float delta)
        {
            List<Func<IContainerProvider, IGameModule>>? toAdd = null;
            List<Func<IContainerProvider, IGameModule>>? toRemoveList = null;
            lock (pendingModulesLock)
            {
                if (modulesToAdd.Count > 0)
                {
                    toAdd = modulesToAdd.ToList();
                    modulesToAdd.Clear();
                }
                if (modulesToRemove.Count > 0)
                {
                    toRemoveList = modulesToRemove.ToList();
                    modulesToRemove.Clear();
                }
            }
            if (toAdd != null)
            {
                foreach (var m in toAdd)
                {
                    var moduleInstance = m(containerProvider);
                    moduleInstance.Initialize();
                    modules.Add((m, moduleInstance, moduleInstance.ViewModel));
                    if (moduleInstance.ViewModel != null)
                        ViewModels.Add(moduleInstance.ViewModel);
                }
            }
            if (toRemoveList != null)
            {
                foreach (var toRemove in toRemoveList)
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
            }
            if (toAdd != null || toRemoveList != null)
                modulesSnapshot = modules.Select(x => x.Item2).ToArray();
            foreach (var module in modules)
            {
                module.Item2.Update(delta);
            }
        }

        private void GameViewOnModuleRemoved(Func<IContainerProvider, IGameModule> m)
        {
            lock (pendingModulesLock)
                modulesToRemove.Add(m);
        }

        private void GameViewOnModuleRegistered(Func<IContainerProvider, IGameModule> m)
        {
            lock (pendingModulesLock)
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
            modulesSnapshot = [];
        }

        public void Render(float delta)
        {
            foreach (var module in modules)
                module.Item2.Render(delta);
        }

        public void RenderGUI()
        {
            foreach (var module in modules)
                module.Item2.RenderGUI();
        }

        public void RenderTransparent()
        {
            foreach (var module in modules)
                module.Item2.RenderTransparent();
        }
    }
}