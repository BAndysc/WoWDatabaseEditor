using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using WDE.Common.Disposables;
using WDE.Module.Attributes;

namespace WDE.MapRenderer
{
    [AutoRegister]
    [SingleInstance]
    public class GameViewService : IGameView
    {
        private List<Func<IGameModule>> modules = new();

        public IEnumerable<Func<IGameModule>> Modules => modules;
        public event Action<Func<IGameModule>>? ModuleRegistered;
        public event Action<Func<IGameModule>>? ModuleRemoved;

        public IDisposable RegisterGameModule(Func<IGameModule> gameModule)
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
    }

    public class GemView : OpenGlControlBase
    {
        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            
        }
    }
}