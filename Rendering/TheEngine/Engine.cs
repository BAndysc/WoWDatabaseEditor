using System;
using System.Runtime.CompilerServices;
using System.Threading;
using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.ECS;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.Managers;

[assembly: InternalsVisibleTo("TheEngine.Test")]
namespace TheEngine
{
    public class Engine : IDisposable
    {
        internal TheDevice Device { get; }

        internal IConfiguration Configuration { get; }

        internal ShaderManager shaderManager { get; }
        public IShaderManager ShaderManager => shaderManager;


        internal MeshManager meshManager { get; }
        public IMeshManager MeshManager => meshManager;


        internal RenderManager renderManager { get; }
        public IRenderManager RenderManager => renderManager;


        internal CameraManager cameraManger { get; }
        public ICameraManager CameraManager => cameraManger;

        internal InputManager inputManager { get; }
        public IInputManager InputManager => inputManager;

        internal LightManager lightManager { get; }
        public ILightManager LightManager => lightManager;
        
        internal TextureManager textureManager { get; }
        public ITextureManager TextureManager => textureManager;

        internal MaterialManager materialManager { get; }
        public IMaterialManager MaterialManager => materialManager;
        public IWindowHost WindowHost { get; }

        internal EntityManager entityManager { get; }
        public IEntityManager EntityManager => entityManager;
        
        internal StatsManager statsManager { get; }
        public IStatsManager StatsManager => statsManager;
        
        internal FontManager fontManager { get; }
        public IFontManager FontManager => fontManager;
        
        internal UIManager uiManager { get; }
        public IUIManager Ui => uiManager;

        public double TotalTime;

        public Engine(IDevice device, IConfiguration configuration, IWindowHost host, bool flipY)
        {
            WindowHost = host;
            //windowHost.Bind(this);

            Configuration = configuration;
            Device = new TheDevice(host, device, false);

            Device.Initialize();

            statsManager = new StatsManager();
            entityManager = new EntityManager();
            
            lightManager = new LightManager(this);
            inputManager = new InputManager(this);
            cameraManger = new CameraManager(this);

            materialManager = new MaterialManager(this);
            shaderManager = new ShaderManager(this);
            meshManager = new MeshManager(this);
            textureManager = new TextureManager(this);
            renderManager = new RenderManager(this, flipY);

            fontManager = new FontManager(this);
            uiManager = new UIManager(this);
        }
        
        public void UpdateGui(float delta)
        {
            uiManager.UpdateGui(delta);
        }
        
        public void RenderGUI()
        {
            Device.device.Debug("  Rendering GUI");
            uiManager.Render();
        }
        
        public NativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, ReadOnlySpan<T> data, BufferInternalFormat format = BufferInternalFormat.None) where T : unmanaged => Device.CreateBuffer<T>(bufferType, data, format);
        public NativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, int size, BufferInternalFormat format = BufferInternalFormat.None) where T : unmanaged => Device.CreateBuffer<T>(bufferType, size, format);
        
        public void Dispose()
        {
            uiManager.Dispose();
            fontManager.Dispose();
            lightManager.Dispose();
            cameraManger.Dispose();
            materialManager.Dispose();
            renderManager.Dispose();
            meshManager.Dispose();
            textureManager.Dispose();
            shaderManager.Dispose();
            entityManager.Dispose();
            Device.Dispose();
        }
    }
}
