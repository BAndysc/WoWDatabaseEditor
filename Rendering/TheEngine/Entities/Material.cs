using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Rendering;
using TheMaths;

namespace TheEngine.Entities
{

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
            BumpResourceGeneration();
        }

        public ref readonly T MaterialData => ref MemoryMarshal.Cast<byte, T>(materialDataBytes)[0];
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

        // Textures sampled bindlessly (their slot index lives in the material data, not a descriptor
        // binding) still need a strong reference somewhere or the managed Texture is GC'd and its
        // native texture finalized, dangling the bindless slot -> GPU page fault. A material anchors
        // such textures here purely for lifetime; nothing in the descriptor path reads this list.
        private readonly List<ITexture> keepAliveTextures = new();

        /// <summary>Row index of this material within its type's MaterialManager.MaterialTypeArray SSBO, or -1 if not registered.</summary>
        internal int MaterialArrayIndex = -1;

        public Span<byte> MaterialDataBytes => materialDataBytes;

        /// <summary>
        /// Monotonic counter bumped whenever this material's constant data, textures or structured
        /// buffers change. The Vulkan set=1 descriptor cache keys on (material, generation) instead
        /// of memcmp-ing the material bytes every draw, so a steady material is a cheap int compare.
        /// </summary>
        internal uint ResourceGeneration { get; private set; }

        protected void BumpResourceGeneration() => ResourceGeneration++;

        public bool BlendingEnabled { get; }

        internal Material(Engine engine, Pipeline pipeline, MaterialHandle materialHandle)
        {
            this.engine = engine;
            Pipeline = pipeline;
            Handle = materialHandle;
            BlendingEnabled = pipeline.Description.BlendState.AttachmentStates[0].BlendEnabled;
        }

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

        /// <summary>Anchors a bindlessly-sampled texture's lifetime to this material (no descriptor
        /// binding). Every sampled texture is bindless now: the shader reads its slot index from the
        /// material data, and this keeps the managed texture alive so its bindless slot isn't dangled.</summary>
        public void KeepAlive(ITexture texture)
        {
            if (!keepAliveTextures.Contains(texture))
                keepAliveTextures.Add(texture);
        }

        /// <summary>A clone samples the same bindless slots as its source, so it must anchor the same
        /// textures (see <see cref="KeepAlive"/>).</summary>
        internal void CopyKeepAlivesFrom(Material source)
        {
            foreach (var texture in source.keepAliveTextures)
                KeepAlive(texture);
        }

        public IShaderPass? GetShaderPass(ShaderPassType passType)
        {
            return passType switch
            {
                ShaderPassType.Forward => this.Pipeline.Shader.ForwardPass,
                // dedicated depth/shadow variant if the shader declares one, else the forward pass
                ShaderPassType.Depth => this.Pipeline.Shader.DepthPass ?? this.Pipeline.Shader.ForwardPass,
                ShaderPassType.Shadow => this.Pipeline.Shader.ShadowPass,
                _ => null
            };
        }
    }
}
