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
using TheEngine.Rendering;
using TheEngine.Resources;
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
            Pipeline pipeline,
            MaterialHandle materialHandle) : base(engine, pipeline, materialHandle)
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
                    var sizeOf = Marshal.SizeOf(field.FieldType);
                    uniformData.Add(new UniformSlotInfo(field.Name, location, offset, sizeOf, field.FieldType));
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

        protected override unsafe void ActivateMoreUniforms(ShaderPass shaderPass, ReadOnlySpan<byte> materialData)
        {
            foreach (var uniform in uniformData)
            {
                var data = materialData.Slice((int)uniform.offset, uniform.size);
                var loc = uniform.globalLocation;
                if (!shaderPass.HasGlobalUniform(loc))
                {
                    continue;
                }
                fixed (byte* ptr = data)
                {
                    if (uniform.type == typeof(int))
                    {
                        ref var value = ref Unsafe.AsRef<int>(ptr);
                        shaderPass.SetUniformInt(loc, value);
                    }
                    else if (uniform.type == typeof(float))
                    {
                        ref var value = ref Unsafe.AsRef<float>(ptr);
                        shaderPass.SetUniform(loc, value);
                    }
                    else if (uniform.type == typeof(Vector3))
                    {
                        ref var value = ref Unsafe.AsRef<Vector3>(ptr);
                        shaderPass.SetUniform(loc, value.X, value.Y, value.Z);
                    }
                    else if (uniform.type == typeof(Vector4))
                    {
                        ref var value = ref Unsafe.AsRef<Vector4>(ptr);
                        shaderPass.SetUniform(loc, value.X, value.Y, value.Z, value.W);
                    }
                    else if (uniform.type == typeof(Matrix))
                    {
                        ref var value = ref Unsafe.AsRef<Matrix>(ptr);
                        shaderPass.SetUniform(loc, value);
                    }
                    else
                        throw new Exception("Unknown type " + uniform.type);
                }
            }
        }

        public ref T MaterialData => ref MemoryMarshal.Cast<byte, T>(materialDataBytes)[0];
    }

    public struct GlobalUniformHandle : IEquatable<GlobalUniformHandle>
    {
        public bool Equals(GlobalUniformHandle other)
        {
            return handle == other.handle;
        }

        public override bool Equals(object? obj)
        {
            return obj is GlobalUniformHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return handle;
        }

        public static bool operator ==(GlobalUniformHandle left, GlobalUniformHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GlobalUniformHandle left, GlobalUniformHandle right)
        {
            return !left.Equals(right);
        }

        private readonly int handle;
        public GlobalUniformHandle(int handle)
        {
            this.handle = handle + 1;
        }

        public int Handle => handle - 1;

        public bool IsEmpty => handle == 0;
    }

    public class Material
    {
        internal record UniformSlotInfo(string name, GlobalUniformHandle globalLocation, IntPtr offset, int size, Type type);
        internal IReadOnlyList<UniformSlotInfo> thisUniformData;

        private static Dictionary<string, GlobalUniformHandle> globalUniformLocations = new();
        private static Dictionary<GlobalUniformHandle, string> globalUniformLocationsReverse = new();

        protected readonly Engine engine;

        public Pipeline Pipeline { get; }
        public MaterialHandle Handle { get; }

        protected byte[] materialDataBytes = Array.Empty<byte>();
        internal Dictionary<GlobalUniformHandle, ITexture> textures { get; } = new();
        internal Dictionary<GlobalUniformHandle, INativeBuffer> structuredBuffers { get; } = new();

        public Span<byte> MaterialDataBytes => materialDataBytes;

        public bool BlendingEnabled { get; }

        internal Material(Engine engine, Pipeline pipeline, MaterialHandle materialHandle)
        {
            this.engine = engine;
            Pipeline = pipeline;
            Handle = materialHandle;
            BlendingEnabled = pipeline.Description.BlendState.AttachmentStates[0].BlendEnabled;
        }

        public void InvalidateShaderCache()
        {
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

        public static GlobalUniformHandle GetUniformLocation(string name)
        {
            if (!globalUniformLocations.TryGetValue(name, out var loc))
            {
                loc = globalUniformLocations[name] = new GlobalUniformHandle(globalUniformLocations.Count);
                globalUniformLocationsReverse[loc] = name;
            }

            return loc;
        }

        public static string? GetUniformName(GlobalUniformHandle globalUniform)
        {
            return globalUniformLocationsReverse.GetValueOrDefault(globalUniform);
        }

        private void Set<T>(Dictionary<GlobalUniformHandle, T> dict, string name, T type)
        {
            var loc = GetUniformLocation(name);
            dict[loc] = type;
        }

        public void SetBuffer(string name, INativeBuffer buffer)
        {
            Set(structuredBuffers, name, buffer);
        }

        public void SetTexture(string name, ITexture texture)
        {
            Set(textures, name, texture);
        }
        
        public ITexture GetTexture(string name)
        {
            return textures[GetUniformLocation(name)];
        }
        
        public INativeBuffer GetBuffer(string name)
        {
            return structuredBuffers[GetUniformLocation(name)];
        }

        // the data span is passed in (rather than read from materialDataBytes) so the
        // deferred path can activate from a record-time snapshot of the same bytes
        protected virtual void ActivateMoreUniforms(ShaderPass shaderPass, ReadOnlySpan<byte> materialData)
        {
        }

        public IShaderPass? GetShaderPass(ShaderPassType passType, bool instanced)
        {
            return (passType, instanced) switch
            {
                (ShaderPassType.Forward, false) => this.Pipeline.Shader.ForwardPass,
                (ShaderPassType.Forward, true) => this.Pipeline.Shader.ForwardInstancedPass,
                (ShaderPassType.Shadow, false) => this.Pipeline.Shader.ShadowPass,
                (ShaderPassType.Shadow, true) => this.Pipeline.Shader.ShadowInstancedPass,
                _ => null
            };
        }

        // called by the command list when binding material resources (BindMaterialResources);
        // on GL the material's textures/buffers/constants are bound as loose uniforms,
        // on Vulkan they will become a descriptor set + push constants
        internal void ActivateUniforms(ShaderPass shaderPass, MaterialInstanceRenderData? instanceData = null)
        {
            int slot = 0;

            foreach (var buffer in structuredBuffers)
            {
                if (instanceData != null && instanceData.structuredBuffers != null &&
                    instanceData.structuredBuffers.ContainsKey(buffer.Key))
                    continue;
                if (!shaderPass.HasGlobalUniform(buffer.Key))
                    continue;
                buffer.Value.Activate(slot);
                shaderPass.SetUniformInt(buffer.Key, slot);
                slot++;
            }
            foreach (var pair in textures)
            {
                if (!shaderPass.HasGlobalUniform(pair.Key))
                    continue;
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
                shaderPass.SetUniformInt(pair.Key, slot);
                slot++;
            }

            ActivateMoreUniforms(shaderPass, materialDataBytes);

            instanceData?.Activate(shaderPass, slot);
        }

        // the deferred-recording path: the dictionary walks and shader-uniform checks already
        // happened when the snapshot was captured, this just executes the recorded binds
        internal void ActivateUniforms(ShaderPass shaderPass, MaterialSnapshot snapshot)
        {
            foreach (var (uniform, slot, buffer) in snapshot.Buffers)
            {
                buffer.Activate(slot);
                shaderPass.SetUniformInt(uniform, slot);
            }
            foreach (var (uniform, slot, texture) in snapshot.Textures)
            {
                var resolved = texture == null ? null : engine.textureManager.GetTextureByHandle(texture.Handle);
                resolved ??= engine.textureManager.GetTextureByHandle(engine.textureManager.EmptyTexture.Handle);
                resolved.Activate(slot);
                shaderPass.SetUniformInt(uniform, slot);
            }
            ActivateMoreUniforms(shaderPass, snapshot.MaterialData.AsSpan(0, snapshot.MaterialDataLength));
        }
        
        public enum StructuredBufferMode
        {
            VertexOnly,
            PixelOnly,
            VertexPixel
        }
    }
}
