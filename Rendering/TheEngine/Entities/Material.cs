using TheAvaloniaOpenGL.Resources;
using TheEngine.Handles;
using TheMaths;

namespace TheEngine.Entities
{
    public enum CullingMode
    {
        Front,
        Back,
        Off
    }
    
    // openGL values on purpose
    public enum Blending
    {
        Zero = 0,
        SrcColor = 768,
        OneMinusSrcColor = 769,
        SrcAlpha = 770,
        OneMinusSrcAlpha = 771,
        DstAlpha = 772,
        OneMinusDstAlpha = 773,
        DstColor = 774,
        OneMinusDstColor = 775,
        SrcAlphaSaturate = 776,
        ConstantColor = 32769,
        OneMinusConstantColor = 32770,
        ConstantAlpha = 32771,
        OneMinusConstantAlpha = 32772,
        Src1Alpha = 34185,
        Src1Color = 35065,
        OneMinusSrc1Color = 35066,
        OneMinusSrc1Alpha = 35067,
        One = 1
    }

    public class Material
    {
        private readonly Engine engine;
        private readonly ShaderHandle shaderHandle;

        private Shader shader;
        public CullingMode Culling = CullingMode.Back;
        public bool BlendingEnabled = false;
        public Blending SourceBlending = Blending.One;
        public Blending DestinationBlending = Blending.Zero;

        internal Shader Shader => shader;

        public ShaderHandle ShaderHandle => shaderHandle;

        internal Dictionary<int, TextureHandle> textureHandles { get; } = new();
        internal Dictionary<int, INativeBuffer> structuredBuffers { get; }
        internal Dictionary<int, float> floatUniforms { get; } = new();
        internal Dictionary<int, Vector4> vector4Uniforms { get; } = new();
        internal Dictionary<int, INativeBuffer> structuredVertexBuffers { get; }
        internal Dictionary<int, INativeBuffer> structuredPixelsBuffers { get; }

        internal int SlotCount => textureHandles.Count + structuredBuffers.Count + structuredPixelsBuffers.Count +
                                  structuredPixelsBuffers.Count;
        
        internal Material(Engine engine, ShaderHandle shaderHandle)
        {
            this.engine = engine;
            this.shaderHandle = shaderHandle;
            this.shader = engine.shaderManager.GetShaderByHandle(shaderHandle);

            structuredBuffers = new Dictionary<int, INativeBuffer>();
            structuredVertexBuffers = new Dictionary<int, INativeBuffer>();
            structuredPixelsBuffers = new Dictionary<int, INativeBuffer>();
        }

        public void SetStructuredBuffer<T>(int index, T[] data, StructuredBufferMode mode = StructuredBufferMode.VertexPixel) where T : unmanaged
        {
            var bufferMode = BufferTypeEnum.StructuredBuffer;
            if (mode == StructuredBufferMode.PixelOnly)
                bufferMode = BufferTypeEnum.StructuredBufferPixelOnly;
            else if (mode == StructuredBufferMode.VertexOnly)
                bufferMode = BufferTypeEnum.StructuredBufferVertexOnly;

            INativeBuffer buffer = engine.Device.CreateBuffer<T>(bufferMode, data);

            if (mode == StructuredBufferMode.PixelOnly)
            {
                structuredPixelsBuffers[index] = buffer;
            }
            else if (mode == StructuredBufferMode.VertexOnly)
            {
                structuredVertexBuffers[index] = buffer;
            }
            else
            {
                structuredBuffers[index] = buffer;
            }
        }

        public int GetUniformLocation(string name)
        {
            var loc = shader.GetUniformLocation(name);
            if (loc == -1)
                throw new Exception("Location " + name + " not found");
            return loc;
        }

        public void SetBuffer<T>(string name, NativeBuffer<T> buffer) where T : unmanaged
        {
            structuredBuffers[GetUniformLocation(name)] = buffer;
        }
        
        public void SetUniform(string name, float value)
        {
            floatUniforms[GetUniformLocation(name)] = value;
        }
        
        public void SetUniform(string name, Vector4 value)
        {
            vector4Uniforms[GetUniformLocation(name)] = value;
        }
        
        public void SetTexture(string name, TextureHandle texture)
        {
            var loc = GetUniformLocation(name);
            textureHandles[loc] = texture;
        }

        public TextureHandle GetTexture(string name)
        {
            return textureHandles[GetUniformLocation(name)];
        }
        
        public void ActivateUniforms()
        {
            int slot = 0;

            // done in RenderManager
            // shader.Activate();
            foreach (var buffer in structuredBuffers)
            {
                buffer.Value.Activate(slot);
                engine.Device.device.Uniform1I(buffer.Key, slot);
                slot++;
            }
            foreach (var pair in textureHandles)
            {
                var texture = engine.textureManager.GetTextureByHandle(pair.Value);
                texture.Activate(slot);
                engine.Device.device.Uniform1I(pair.Key, slot);
                slot++;
            }

            foreach (var floats in floatUniforms)
            {
                engine.Device.device.Uniform1f(floats.Key, floats.Value);
            }
            foreach (var vector in vector4Uniforms)
            {
                engine.Device.device.Uniform4f(vector.Key, vector.Value.X, vector.Value.Y, vector.Value.Z, vector.Value.W);
            }
        }
        
        public enum StructuredBufferMode
        {
            VertexOnly,
            PixelOnly,
            VertexPixel
        }
    }
}
