using OpenGLBindings;
using SixLabors.ImageSharp.PixelFormats;
using TheAvaloniaOpenGL.Resources;
using TheMaths;

namespace TheAvaloniaOpenGL
{
    public class TheDevice : IDisposable
    {
        private readonly bool dxDebug;

        public IDevice device;
        // private SwapChain swapChain;
        // private RenderTargetView renderTargetView;
        // private Texture2D depthStencilBuffer;
        // private DepthStencilView depthStencilView;
        // private RasterizerState rasterizerState;

        public TheDevice(IWindowHost windowHost, IDevice device, bool dxDebug)
        {
            this.device = device;
            this.dxDebug = dxDebug;
        }

        public void Initialize()
        {
            //var flags = DeviceCreationFlags.BgraSupport;
            //if (dxDebug)
            //    flags |= DeviceCreationFlags.Debug;

            
            // var swapChainDescription = new SwapChainDescription
            // {
            //     OutputHandle = outputHandle,
            //     BufferCount = 1,
            //     Flags = SwapChainFlags.AllowModeSwitch,
            //     IsWindowed = true,
            //     ModeDescription = new ModeDescription(size.Width, size.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
            //     SampleDescription = new SampleDescription(1, 0),
            //     SwapEffect = SwapEffect.Discard,
            //     Usage = Usage.RenderTargetOutput,
            // };
            
            //Device.CreateWithSwapChain(DriverType.Hardware, flags, new FeatureLevel[] { FeatureLevel.Level_11_0 }, swapChainDescription, out device, out swapChain);

            // New RenderTargetView from the backbuffer
            /*var backBuffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);
            renderTargetView = new RenderTargetView(device, backBuffer);
            backBuffer.Dispose();

            Texture2DDescription depthBufferDesc = new Texture2DDescription()
            {
                Width = size.Width,
                Height = size.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D24_UNorm_S8_UInt,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            depthStencilBuffer = new Texture2D(device, depthBufferDesc);

            DepthStencilViewDescription depthStencilViewDesc = new DepthStencilViewDescription()
            {
                Format = Format.D24_UNorm_S8_UInt,
                Dimension = DepthStencilViewDimension.Texture2D,
                Texture2D = new DepthStencilViewDescription.Texture2DResource()
                {
                    MipSlice = 0
                }
            };
            depthStencilView = new DepthStencilView(device, depthStencilBuffer, depthStencilViewDesc);

            RasterizerStateDescription rasterDesc = new RasterizerStateDescription()
            {
                IsAntialiasedLineEnabled = false,
                CullMode = CullMode.Back,
                DepthBias = 0,
                DepthBiasClamp = .0f,
                IsDepthClipEnabled = true,
                FillMode = FillMode.Solid, //FillMode.Wireframe,
                IsFrontCounterClockwise = false,
                IsMultisampleEnabled = false,
                IsScissorEnabled = false,
                SlopeScaledDepthBias = .0f
            };
            rasterizerState = new RasterizerState(device, rasterDesc);

            device.ImmediateContext.Rasterizer.SetViewport(0, 0, size.Width, size.Height);
            SetRenderTexture(null);
            device.ImmediateContext.Rasterizer.State = rasterizerState;*/
        }

        // public void DrawIndexed(object indexCount, int v1, int v2)
        // {
        //     throw new NotImplementedException();
        // }

        // Safe multithread call
        public Sampler CreateSampler(/* todo */)
        {
            return new Sampler(device);
        }

        // Safe multithread call
        public Texture CreateTexture(int width, int height, Rgba32[][] pixels, bool generateMips)
        {
            return new Texture(device, pixels, width, height, generateMips);
        }
        
        public unsafe Texture CreateTexture(int width, int height, Rgba32* pixels, bool generateMips)
        {
            return new Texture(device, pixels, width, height, generateMips);
        }
        
        public Texture CreateTexture(int width, int height, Vector4[] pixels)
        {
            return new Texture(device, pixels, width, height);
        }
        
        public Texture CreateTexture(int width, int height, float[] pixels)
        {
            return new Texture(device, pixels, width, height);
        }
        
        public Texture CreateTexture(int width, int height, uint[]? pixels, TextureFormat format = TextureFormat.R8G8B8A8)
        {
            return new Texture(device, pixels, width, height, format);
        }

        // Safe multithread call
        public TextureArray CreateTextureArray(int width, int height, Rgba32[][][] pixels)
        {
            return new TextureArray(device, width, height, pixels);
        }

        // Safe multithread call
        public TextureCube CreateTextureCube(int width, int height, int[][] pixels)
        {
            return new TextureCube(device, pixels, width, height);
        }

        // Safe multithread call
        public Shader CreateShader(string path, string[] includePaths, bool instanced)
        {
            return new Shader(device, path, includePaths, instanced);
        }

        // Safe multithread call
        internal RenderTexture CreateRenderTexture(int width, int height, int colorAttachments = 1, Texture? depthTexture = null)
        {
            return new RenderTexture(device, width, height, colorAttachments, depthTexture);
        }

        // Safe multithread call
        internal RenderTexture CreateRenderTexture(Texture colorAttachment, Texture depthTexture)
        {
            return new RenderTexture(device, colorAttachment, depthTexture);
        }

        // Safe multithread call
        public NativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, int size, BufferInternalFormat format = BufferInternalFormat.None) where T : unmanaged
        {
            return new NativeBuffer<T>(device, bufferType, size, format);
        }
        
        public NativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, ReadOnlySpan<T> data, BufferInternalFormat format = BufferInternalFormat.None) where T : unmanaged
        {
            return new NativeBuffer<T>(device, bufferType, data, format);
        }

        public DepthStencil CreateDepthStencilState(bool zWrite)
        {
            return new DepthStencil(device, zWrite);
        }

        private Color4 gray = new Color4(0.4f, 0.4f, 0.4f, 1);

        // call only form render thread
        public void RenderClearBuffer()
        {
            device.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //device.ImmediateContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1, 0);
            //device.ImmediateContext.ClearRenderTargetView(renderTargetView, gray);
        }

        public void DrawLineMesh(int verticesCount, int startLocation)
        {
            device.DrawArrays(PrimitiveType.Lines, startLocation, new IntPtr(verticesCount));
        }
        
        private DrawElementsType ToOpenGlIndexType(IndexType type, out int bytesSize)
        {
            switch (type)
            {
                case IndexType.Short:
                    bytesSize = 2;
                    return DrawElementsType.UnsignedShort;
                case IndexType.Int:
                    bytesSize = 4;
                    return DrawElementsType.UnsignedInt;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        // call only form render thread
        public void DrawIndexed(int indexCount, int startIndexLocation, int baseVertexLocationBytes, IndexType type)
        {
            if (baseVertexLocationBytes == 0)
                device.DrawElements(PrimitiveType.Triangles, indexCount, ToOpenGlIndexType(type, out var bytesSize), new IntPtr(startIndexLocation * bytesSize));
            else
                device.DrawElementsBaseVertex(PrimitiveType.Triangles, indexCount, ToOpenGlIndexType(type, out var bytesSize), new IntPtr(startIndexLocation * bytesSize), baseVertexLocationBytes);
            //device.ImmediateContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
        }

        // call only form render thread
        public void DrawIndexedInstanced(int indexCountPerInstance, int instanceCount, int startIndexLocation, int baseVertexLocation, int startInstanceLocation, IndexType type)
        {
            device.DrawElementsInstanced(PrimitiveType.Triangles, indexCountPerInstance, ToOpenGlIndexType(type, out var bytesSize), new IntPtr(startIndexLocation * bytesSize), instanceCount);
            //device.ImmediateContext.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
        }

        /// <summary>
        /// Sets current render texture.
        /// call only from render thread
        /// </summary>
        /// <param name="renderTexture">render texture or null, then it resets to back buffer</param>
        /// <param name="destFramebuffer"></para>
        internal void SetRenderTexture(RenderTexture? renderTexture, int destFramebuffer)
        {
            if (renderTexture == null)
                device.BindFramebuffer(FramebufferTarget.Framebuffer, destFramebuffer);   
            else
                renderTexture.ActivateFrameBuffer(1);
            //RenderTargetView view = renderTexture == null ? renderTargetView : renderTexture.TargetView;
            //device.ImmediateContext.OutputMerger.SetTargets(depthStencilView, view);
        }


        // Call only from render thread
        public void RenderBlitBuffer()
        {
            throw new Exception();
            //swapChain.Present(1, PresentFlags.None);
        }

        public void Dispose()
        {
            /*rasterizerState.Dispose();
            depthStencilView.Dispose();
            depthStencilBuffer.Dispose();
            renderTargetView.Dispose();
            swapChain.Dispose();
            device.Dispose();*/
        }
    }
}
