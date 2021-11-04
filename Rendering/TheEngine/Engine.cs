using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.Managers;

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
        public InputManager InputManager => inputManager;

        internal LightManager lightManager { get; }
        public ILightManager LightManager => lightManager;
        
        internal TextureManager textureManager { get; }
        public ITextureManager TextureManager => textureManager;

        internal MaterialManager materialManager { get; }
        public IMaterialManager MaterialManager => materialManager;
        public IWindowHost WindowHost { get; }

        private Thread renderThread;
        
        private volatile bool isDisposing;

        public double TotalTime;

        public Engine(IDevice device, IConfiguration configuration, IWindowHost host)
        {
            WindowHost = host;
            //windowHost.Bind(this);

            Configuration = configuration;
            Device = new TheDevice(host, device, false);

            Device.Initialize();

            lightManager = new LightManager(this);
            inputManager = new InputManager(this);
            cameraManger = new CameraManager(this);

            materialManager = new MaterialManager(this);
            shaderManager = new ShaderManager(this);
            meshManager = new MeshManager(this);
            renderManager = new RenderManager(this);

            textureManager = new TextureManager(this);
        }

        public void Render()
        {
            
        }
        
        public NativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, int size) where T : unmanaged => Device.CreateBuffer<T>(bufferType, size);
        public NativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, ReadOnlySpan<T> data) where T : unmanaged => Device.CreateBuffer<T>(bufferType, data);

        public void Dispose()
        {
            isDisposing = true;
            renderManager.Dispose();
            lightManager.Dispose();
            cameraManger.Dispose();
            materialManager.Dispose();
            meshManager.Dispose();
            textureManager.Dispose();
            shaderManager.Dispose();
            Device.Dispose();
        }
    }
}
