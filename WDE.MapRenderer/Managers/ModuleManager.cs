using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WDE.MVVM.Observable;

namespace WDE.MapRenderer.Managers
{
    public class ModuleManager : System.IDisposable
    {
        private readonly IGameContext gameContext;
        private readonly IGameView gameView;

        private HashSet<Func<IGameModule>> modulesToAdd = new();
        private HashSet<Func<IGameModule>> modulesToRemove = new();
        private List<(Func<IGameModule>, IGameModule, object?)> modules = new();
        public ObservableCollection<object> ViewModels { get; } = new();

        public ModuleManager(IGameContext gameContext, IGameView gameView)
        {
            this.gameContext = gameContext;
            this.gameView = gameView;
            
            foreach (var m in gameView.Modules)
            {
                var moduleInstance = m();
                moduleInstance.Initialize(gameContext);
                modules.Add((m, moduleInstance, moduleInstance.ViewModel));
                if (moduleInstance.ViewModel != null)
                    ViewModels.Add(moduleInstance.ViewModel);
            }

            gameView.ModuleRegistered += GameViewOnModuleRegistered;
            gameView.ModuleRemoved += GameViewOnModuleRemoved;
        }

        public void Update(float delta)
        {
            foreach (var m in modulesToAdd)
            {
                var moduleInstance = m();
                moduleInstance.Initialize(gameContext);
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

        private void GameViewOnModuleRemoved(Func<IGameModule> m)
        {
            modulesToRemove.Add(m);
        }

        private void GameViewOnModuleRegistered(Func<IGameModule> m)
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