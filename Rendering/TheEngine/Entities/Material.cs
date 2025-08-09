using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenGLBindings;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.Handles;
using TheEngine.Interfaces;
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
    public enum DepthCompare
    {
        Never = 0x200,
        Less,
        Equal,
        Lequal,
        Greater,
        Notequal,
        Gequal,
        Always
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

    public class Material<T> : Material where T : unmanaged
    {
        private static List<UniformSlotInfo> uniformData;

        internal unsafe Material(Engine engine,
            ShaderHandle shaderHandle,
            ShaderHandle? instancedShaderHandle,
            MaterialHandle materialHandle) : base(engine, shaderHandle, instancedShaderHandle, materialHandle)
        {
            var size = sizeof(T);
            if ((size % 16) != 0)
                throw new Exception("Material data size must be multiple of 16");

            materialDataBytes = new byte[sizeof(T)];

            if (uniformData == null)
            {
                uniformData = new();
                var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    if (field.Name.Contains("padding", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var offset = Marshal.OffsetOf(typeof(T), field.Name);
                    var location = GetUniformLocation(field.Name);
                    var instancedLocation = GetInstancedUniformLocation(field.Name);
                    var sizeOf = Marshal.SizeOf(field.FieldType);
                    uniformData.Add(new UniformSlotInfo(field.Name, location, instancedLocation, offset, sizeOf, field.FieldType));
                }
            }

            thisUniformData = uniformData;
        }

        public void SetMaterialData(ref T materialData)
        {
            Span<T> materialDataSpan = MemoryMarshal.CreateSpan(ref materialData, 1);
            var bytesSpan = MemoryMarshal.Cast<T, byte>(materialDataSpan);

            bytesSpan.CopyTo(this.materialDataBytes);
        }

        public override unsafe void ActivateUniforms(bool instanced, MaterialInstanceRenderData? instanceData = null)
        {
            base.ActivateUniforms(instanced, instanceData);
            foreach (var uniform in uniformData)
            {
                var data = materialDataBytes.AsSpan((int)uniform.offset, uniform.size);
                var loc = (instanced ? uniform.instancedLocation : uniform.location) ?? throw new Exception("Unknown variable");
                var shader = (instanced ? InstancedShader : Shader) ?? throw new Exception("Unknown shader");
                fixed (byte* ptr = data)
                {
                    if (uniform.type == typeof(int))
                    {
                        ref var value = ref Unsafe.AsRef<int>(ptr);
                        shader.SetUniformInt(loc, value);
                    }
                    else if (uniform.type == typeof(float))
                    {
                        ref var value = ref Unsafe.AsRef<float>(ptr);
                        shader.SetUniform(loc, value);
                    }
                    else if (uniform.type == typeof(Vector3))
                    {
                        ref var value = ref Unsafe.AsRef<Vector3>(ptr);
                        shader.SetUniform(loc, value.X, value.Y, value.Z);
                    }
                    else if (uniform.type == typeof(Vector4))
                    {
                        ref var value = ref Unsafe.AsRef<Vector4>(ptr);
                        shader.SetUniform(loc, value.X, value.Y, value.Z, value.W);
                    }
                    else if (uniform.type == typeof(Matrix))
                    {
                        ref var value = ref Unsafe.AsRef<Matrix>(ptr);
                        shader.SetUniform(loc, value);
                    }
                    else
                        throw new Exception("Unknown type " + uniform.type);
                }
            }
        }

        public ref T MaterialData => ref MemoryMarshal.Cast<byte, T>(materialDataBytes)[0];
    }

    public class Material
    {
        internal record UniformSlotInfo(string name, int location, int? instancedLocation, IntPtr offset, int size, Type type);
        internal IReadOnlyList<UniformSlotInfo> thisUniformData;

        protected readonly Engine engine;
        private readonly ShaderHandle shaderHandle;
        private readonly ShaderHandle? instancedShaderHandle;

        private Shader shader;
        private Shader? instancedShader;
        public bool ZWrite = true;
        public DepthCompare DepthTesting = DepthCompare.Lequal;
        public CullingMode Culling = CullingMode.Back;
        public bool BlendingEnabled = false;
        public Blending SourceBlending = Blending.One;
        public Blending DestinationBlending = Blending.Zero;

        internal Shader Shader => shader;
        internal Shader? InstancedShader => instancedShader;

        public MaterialHandle Handle { get; }
        public ShaderHandle ShaderHandle => shaderHandle;

        protected byte[] materialDataBytes = Array.Empty<byte>();
        internal Dictionary<int, ITexture> textures { get; } = new();
        internal Dictionary<int, INativeBuffer> structuredBuffers { get; } = new();
        internal Dictionary<int, ITexture> instancedTextureHandles { get; } = new();
        internal Dictionary<int, INativeBuffer> instancedStructuredBuffers { get; } = new();

        public Span<byte> MaterialDataBytes => materialDataBytes;

        internal Material(Engine engine, ShaderHandle shaderHandle, ShaderHandle? instancedShaderHandle, MaterialHandle materialHandle)
        {
            this.engine = engine;
            Handle = materialHandle;
            this.shaderHandle = shaderHandle;
            this.instancedShaderHandle = instancedShaderHandle;
            this.shader = engine.shaderManager.GetShaderByHandle(shaderHandle);
            this.instancedShader = instancedShaderHandle.HasValue ? engine.shaderManager.GetShaderByHandle(instancedShaderHandle.Value) : null;
            ZWrite = shader.ZWrite;
            DepthTesting = (DepthCompare)shader.DepthTest;
        }

        public void InvalidateShaderCache()
        {
            shader = engine.shaderManager.GetShaderByHandle(shaderHandle);
            instancedShader = instancedShaderHandle.HasValue ? engine.shaderManager.GetShaderByHandle(instancedShaderHandle.Value) : null;
        }

        // public void SetStructuredBuffer<T>(int index, T[] data, StructuredBufferMode mode = StructuredBufferMode.VertexPixel) where T : unmanaged
        // {
        //     var bufferMode = BufferTypeEnum.StructuredBuffer;
        //     if (mode == StructuredBufferMode.PixelOnly)
        //         bufferMode = BufferTypeEnum.StructuredBufferPixelOnly;
        //     else if (mode == StructuredBufferMode.VertexOnly)
        //         bufferMode = BufferTypeEnum.StructuredBufferVertexOnly;
        //
        //     INativeBuffer buffer = engine.Device.CreateBuffer<T>(bufferMode, data);
        //
        //     if (mode == StructuredBufferMode.PixelOnly)
        //     {
        //         structuredPixelsBuffers[index] = buffer;
        //     }
        //     else if (mode == StructuredBufferMode.VertexOnly)
        //     {
        //         structuredVertexBuffers[index] = buffer;
        //     }
        //     else
        //     {
        //         structuredBuffers[index] = buffer;
        //     }
        // }

        public bool HasInstanceUniform(string name)
        {
            return instancedShader != null && instancedShader.GetUniformLocation(name).HasValue;
        }
        
        public int? GetInstancedUniformLocation(string name)
        {
            if (instancedShader == null)
                return null;
            var instancedLoc = instancedShader.GetUniformLocation(name);
            if (!instancedLoc.HasValue)
                throw new Exception("Location " + name + " not found");
            return instancedLoc.Value;
        }
        
        public int GetUniformLocation(string name)
        {
            var loc = shader.GetUniformLocation(name);
            if (!loc.HasValue)
                throw new Exception("Location " + name + " not found");
            return loc.Value;
        }

        private void Set<T>(Dictionary<int, T> dict, Dictionary<int, T> instanced, string name, T type)
        {
            var loc = GetUniformLocation(name);
            if (loc != -1)
                dict[loc] = type;
            var instLoc = GetInstancedUniformLocation(name);
            if (instLoc.HasValue && instLoc != -1)
                instanced[instLoc.Value] = type;
        }

        public void SetBuffer(string name, INativeBuffer buffer)
        {
            Set(structuredBuffers, instancedStructuredBuffers, name, buffer);
        }

        public void SetTexture(string name, ITexture texture)
        {
            Set(textures, instancedTextureHandles, name, texture);
        }
        
        public ITexture GetTexture(string name)
        {
            return textures[GetUniformLocation(name)];
        }
        
        public INativeBuffer GetBuffer(string name)
        {
            return structuredBuffers[GetUniformLocation(name)];
        }

        public virtual void ActivateUniforms(bool instanced, MaterialInstanceRenderData? instanceData = null)
        {
            int slot = 0;
            // done in RenderManager
            // shader.Activate();

            if (instanced)
            {
                foreach (var buffer in instancedStructuredBuffers)
                {
                    if (instanceData != null && instanceData.instancedStructuredBuffers != null &&
                        instanceData.instancedStructuredBuffers.ContainsKey(buffer.Key))
                        continue;
                    buffer.Value.Activate(slot);
                    instancedShader!.SetUniformInt(buffer.Key, slot);
                    slot++;
                }
                foreach (var pair in instancedTextureHandles)
                {
                    var texture = engine.textureManager.GetTextureByHandle(pair.Value.Handle);
                    texture.Activate(slot);
                    instancedShader!.SetUniformInt(pair.Key, slot);
                    slot++;
                }
            }
            else
            {
                foreach (var buffer in structuredBuffers)
                {
                    if (instanceData != null && instanceData.structuredBuffers != null &&
                        instanceData.structuredBuffers.ContainsKey(buffer.Key))
                        continue;
                    buffer.Value.Activate(slot);
                    shader.SetUniformInt(buffer.Key, slot);
                    slot++;
                }
                foreach (var pair in textures)
                {
                    if (pair.Value != null)
                    {
                        var texture = engine.textureManager.GetTextureByHandle(pair.Value.Handle);
                        if (texture == null)
                            texture = engine.textureManager.GetTextureByHandle(engine.textureManager.EmptyTexture.Handle);
                        texture.Activate(slot);
                    }
                    else
                    {
                        var texture = engine.textureManager.GetTextureByHandle(engine.textureManager.EmptyTexture.Handle);
                        texture.Activate(slot);
                    }
                    shader.SetUniformInt(pair.Key, slot);
                    slot++;
                }
            }

            instanceData?.Activate(this, instanced, slot);
        }
        
        public enum StructuredBufferMode
        {
            VertexOnly,
            PixelOnly,
            VertexPixel
        }
    }
}
