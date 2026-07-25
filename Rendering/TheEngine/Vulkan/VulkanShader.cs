using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Silk.NET.Vulkan;
using TheEngine.Resources;
using TheEngine.Entities;

namespace TheEngine.Vulkan;

/// <summary>
/// Vulkan shader pass: SPIR-V modules (compiled by shelling out to glslc) plus the
/// reflected material resource interface.
///
/// Descriptor convention (fixed across the whole engine):
///  - set 0, binding 0: SceneData UBO (dynamic offset), binding 1: ObjectData UBO (dynamic offset),
///  - set 1, binding 0: MaterialData UBO (the material's POD struct, std140-compatible),
///  - set 1, binding 1+: sampler2D (combined image sampler) and samplerBuffer/usamplerBuffer/
///    isamplerBuffer (uniform texel buffer) resources, with explicit bindings in the source.
/// Reflection is a regex parse of the preprocessed source - the dialect is our own, so
/// declarations are guaranteed to be in the canonical layout(...) form.
/// </summary>
internal sealed unsafe class VulkanShaderPass : IShaderPass, IDisposable
{
    internal readonly struct ResourceBinding
    {
        public readonly GlobalUniformHandle Uniform;
        public readonly uint Binding;
        public readonly DescriptorType Type;

        public ResourceBinding(GlobalUniformHandle uniform, uint binding, DescriptorType type)
        {
            Uniform = uniform;
            Binding = binding;
            Type = type;
        }
    }

    private readonly VulkanContext ctx;

    internal ShaderModule VertexModule;
    internal ShaderModule FragmentModule;
    internal DescriptorSetLayout Set1Layout;
    internal PipelineLayout PipelineLayout;
    internal readonly List<ResourceBinding> Bindings = new();
    internal bool HasMaterialData;
    internal int MaterialDataSize;
    internal string ShaderName;

    private readonly HashSet<GlobalUniformHandle> knownUniforms = new();
    internal readonly Dictionary<string, ShaderVariableType> UniformTypes = new();

    internal VulkanShaderPass(VulkanContext ctx, DescriptorSetLayout set0Layout, DescriptorSetLayout bindlessLayout, DescriptorSetLayout gameSet3Layout, ShaderData shaderData,
        string shaderFile, bool instanced, string passDefine)
    {
        this.ctx = ctx;
        ShaderName = Path.GetFileNameWithoutExtension(shaderFile);

        var vertexDefines = new List<string> { "VERTEX_SHADER", passDefine };
        var vertexSource = ShaderSource.ParseShader(shaderData.Vertex.Path, true, vertexDefines.ToArray());

        var fragmentDefines = new List<string> { "PIXEL_SHADER", passDefine };
        var fragmentSource = ShaderSource.ParseShader(shaderData.Pixel.Path, true, fragmentDefines.ToArray());

        VertexModule = Compile(vertexSource, "vert", shaderData.Vertex.Path);
        FragmentModule = Compile(fragmentSource, "frag", shaderData.Pixel.Path);

        // reflection must not see declarations in inactive #ifdef blocks (e.g. SHADOW_PASS-only
        // bindings when compiling the FORWARD_PASS variant) - glslc evaluates the defines
        // during compilation, mirror that here
        Reflect(StripInactiveBlocks(vertexSource, vertexDefines));
        Reflect(StripInactiveBlocks(fragmentSource, fragmentDefines));

        CreateLayouts(set0Layout, bindlessLayout, gameSet3Layout);
    }

    /// <summary>Drops lines inside #ifdef/#ifndef blocks whose condition is false for the given defines.</summary>
    private static string StripInactiveBlocks(string source, IReadOnlyCollection<string> defines)
    {
        var result = new System.Text.StringBuilder(source.Length);
        // each entry: (emitting state of the enclosing block, whether any branch of this block was taken)
        var stack = new Stack<(bool parent, bool taken)>();
        bool emitting = true;
        foreach (var rawLine in source.Split('\n'))
        {
            var line = rawLine.AsSpan().TrimStart();
            if (line.StartsWith("#ifdef") || line.StartsWith("#ifndef"))
            {
                bool negated = line.StartsWith("#ifndef");
                var name = line.Slice(negated ? 7 : 6).Trim().ToString();
                bool cond = defines.Contains(name) != negated;
                stack.Push((emitting, emitting && cond));
                emitting = emitting && cond;
            }
            else if (line.StartsWith("#if "))
            {
                // supports `#if defined(A)`, `#if defined(A) || defined(B)`, `#if defined(A) && defined(B)`
                var condStr = line.Slice(4).Trim().ToString();
                bool isOr = condStr.Contains("||");
                var parts = condStr.Split(isOr ? "||" : "&&");
                bool cond = !isOr;
                foreach (var part in parts)
                {
                    var m = Regex.Match(part, @"defined\s*\(\s*(\w+)\s*\)");
                    bool partCond = m.Success && defines.Contains(m.Groups[1].Value);
                    cond = isOr ? cond || partCond : cond && partCond;
                }
                stack.Push((emitting, emitting && cond));
                emitting = emitting && cond;
            }
            else if (line.StartsWith("#else"))
            {
                var (parent, taken) = stack.Pop();
                emitting = parent && !taken;
                stack.Push((parent, taken || emitting));
            }
            else if (line.StartsWith("#endif"))
            {
                emitting = stack.Pop().parent;
            }
            else if (emitting)
            {
                result.Append(rawLine);
                result.Append('\n');
            }
        }
        return result.ToString();
    }

    private void Reflect(string source)
    {
        // set 1 holds only buffers now (all textures are bindless in set 2; the old samplerBuffer/
        // texel-fetch buffers are std430 SSBOs), so reflection only walks the std430 buffer blocks
        // and the optional binding-0 MaterialData UBO below.
        foreach (Match m in Regex.Matches(source,
                     @"layout\s*\(\s*std430\s*,\s*set\s*=\s*1\s*,\s*binding\s*=\s*(\d+)\s*\)\s*readonly\s+buffer\s+(\w+)\s*\{"))
        {
            var binding = uint.Parse(m.Groups[1].Value);
            var name = m.Groups[2].Value;
            var uniform = Material.GetUniformLocation(name);
            if (!knownUniforms.Add(uniform))
                continue;
            Bindings.Add(new ResourceBinding(uniform, binding, DescriptorType.StorageBuffer));
        }

        var block = Regex.Match(source,
            @"layout\s*\(\s*std140\s*,\s*set\s*=\s*1\s*,\s*binding\s*=\s*0\s*\)\s*uniform\s+MaterialData\s*\{([^}]*)\}");
        if (block.Success)
        {
            HasMaterialData = true;
            int offset = 0;
            foreach (Match member in Regex.Matches(block.Groups[1].Value, @"(int|uint|float|vec2|vec3|vec4|mat4)\s+(\w+)\s*;"))
            {
                var type = member.Groups[1].Value;
                var name = member.Groups[2].Value;
                var (size, align) = type switch
                {
                    "int" or "uint" or "float" => (4, 4),
                    "vec2" => (8, 8),
                    "vec3" => (12, 16),
                    "vec4" => (16, 16),
                    "mat4" => (64, 16),
                    _ => throw new Exception($"unsupported MaterialData member type {type}"),
                };
                offset = (offset + align - 1) / align * align + size;
                knownUniforms.Add(Material.GetUniformLocation(name));
                if (!name.Contains("padding", StringComparison.OrdinalIgnoreCase))
                {
                    UniformTypes[name] = type switch
                    {
                        "int" or "uint" => ShaderVariableType.Int,
                        "float" => ShaderVariableType.Float,
                        "vec2" => ShaderVariableType.Float2,
                        "vec3" => ShaderVariableType.Float3,
                        "vec4" => ShaderVariableType.Float4,
                        _ => ShaderVariableType.Matrix,
                    };
                }
            }
            MaterialDataSize = Math.Max(MaterialDataSize, (offset + 15) / 16 * 16);
        }

        // Step B form: struct MaterialData { ... }; layout(std430, set=1, binding=0) readonly
        // buffer MaterialDataArray { MaterialData materials[]; }; - the SSBO binding itself is
        // already registered by the generic std430 buffer regex above; this only populates
        // UniformTypes/knownUniforms for the material editor's per-field UI.
        var arrayBlock = Regex.Match(source,
            @"struct\s+MaterialData\s*\{([^}]*)\}\s*;\s*layout\s*\(\s*std430\s*,\s*set\s*=\s*1\s*,\s*binding\s*=\s*0\s*\)\s*readonly\s+buffer\s+MaterialDataArray\s*\{\s*MaterialData\s+materials\s*\[\s*\]\s*;\s*\}");
        if (arrayBlock.Success)
        {
            foreach (Match member in Regex.Matches(arrayBlock.Groups[1].Value, @"(int|uint|float|vec2|vec3|vec4|mat4)\s+(\w+)\s*;"))
            {
                var type = member.Groups[1].Value;
                var name = member.Groups[2].Value;
                knownUniforms.Add(Material.GetUniformLocation(name));
                if (!name.Contains("padding", StringComparison.OrdinalIgnoreCase))
                {
                    UniformTypes[name] = type switch
                    {
                        "int" or "uint" => ShaderVariableType.Int,
                        "float" => ShaderVariableType.Float,
                        "vec2" => ShaderVariableType.Float2,
                        "vec3" => ShaderVariableType.Float3,
                        "vec4" => ShaderVariableType.Float4,
                        _ => ShaderVariableType.Matrix,
                    };
                }
            }
        }
    }

    private void CreateLayouts(DescriptorSetLayout set0Layout, DescriptorSetLayout bindlessLayout, DescriptorSetLayout gameSet3Layout)
    {
        var bindings = new List<DescriptorSetLayoutBinding>();
        if (HasMaterialData)
            bindings.Add(new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            });
        foreach (var binding in Bindings)
            bindings.Add(new DescriptorSetLayoutBinding
            {
                Binding = binding.Binding,
                DescriptorType = binding.Type,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            });

        var bindingArray = bindings.ToArray();
        fixed (DescriptorSetLayoutBinding* pBindings = bindingArray)
        {
            var layoutInfo = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)bindingArray.Length,
                PBindings = pBindings,
            };
            VulkanContext.Check(ctx.vk.CreateDescriptorSetLayout(ctx.Device, in layoutInfo, null, out Set1Layout), "set1 layout");
        }

        var setLayouts = stackalloc DescriptorSetLayout[4] { set0Layout, Set1Layout, bindlessLayout, gameSet3Layout };
        // a small fragment push constant carrying a per-draw bindless texture index. Only ImGui
        // declares/uses it (to pick its texture per draw command without rewriting set 1); every
        // other graphics shader simply leaves it unused. Kept on all layouts so the range is
        // uniform and pipeline layouts stay interchangeable.
        var pushConstantRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.FragmentBit,
            Offset = 0,
            Size = sizeof(int),
        };
        var pipelineLayoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 4,
            PSetLayouts = setLayouts,
            PushConstantRangeCount = 1,
            PPushConstantRanges = &pushConstantRange,
        };
        VulkanContext.Check(ctx.vk.CreatePipelineLayout(ctx.Device, in pipelineLayoutInfo, null, out PipelineLayout), "pipeline layout");
    }

    private ShaderModule Compile(string source, string stage, string fileName)
    {
        var spirv = GlslCompiler.Compile(source, stage, fileName);
        fixed (byte* pCode = spirv)
        {
            var info = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spirv.Length,
                PCode = (uint*)pCode,
            };
            VulkanContext.Check(ctx.vk.CreateShaderModule(ctx.Device, in info, null, out var module), "shader module " + fileName);
            return module;
        }
    }

    public bool HasGlobalUniform(GlobalUniformHandle globalId) => knownUniforms.Contains(globalId);

    public void Dispose()
    {
        var vk = ctx.vk;
        var device = ctx.Device;
        var vert = VertexModule;
        var frag = FragmentModule;
        var set1 = Set1Layout;
        var layout = PipelineLayout;
        ctx.DestroyLater(() =>
        {
            vk.DestroyShaderModule(device, vert, null);
            vk.DestroyShaderModule(device, frag, null);
            vk.DestroyDescriptorSetLayout(device, set1, null);
            vk.DestroyPipelineLayout(device, layout, null);
        });
    }
}

/// <summary>Compiles our Vulkan-GLSL dialect to SPIR-V by shelling out to glslc (interim until a vendored compiler).</summary>
internal static class GlslCompiler
{
    private static string? glslcPath;

    private static string FindGlslc()
    {
        if (glslcPath != null)
            return glslcPath;
        var candidates = new List<string> { "glslc", "/usr/local/bin/glslc", "/opt/homebrew/bin/glslc" };
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var sdkRoot = Path.Combine(home, "VulkanSDK");
        if (Directory.Exists(sdkRoot))
            foreach (var version in Directory.GetDirectories(sdkRoot))
                candidates.Add(Path.Combine(version, "macOS", "bin", "glslc"));
        foreach (var candidate in candidates)
        {
            try
            {
                using var probe = Process.Start(new ProcessStartInfo(candidate, "--version")
                    { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false });
                probe!.WaitForExit();
                if (probe.ExitCode == 0)
                    return glslcPath = candidate;
            }
            catch
            {
                // try the next candidate
            }
        }
        throw new Exception("glslc not found - install the Vulkan SDK or shaderc");
    }

    public static byte[] Compile(string source, string stage, string fileName)
    {
        var inFile = Path.GetTempFileName();
        var outFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(inFile, source);
            var psi = new ProcessStartInfo(FindGlslc(),
                $"-fshader-stage={stage} --target-env=vulkan1.3 \"{inFile}\" -o \"{outFile}\"")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            using var process = Process.Start(psi)!;
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                int lineNo = 0;
                foreach (var line in source.Split('\n'))
                    Console.WriteLine($"{++lineNo,5}: {line}");
                throw new Exception($"glslc failed for {fileName} ({stage}):\n{stderr}");
            }
            return File.ReadAllBytes(outFile);
        }
        finally
        {
            File.Delete(inFile);
            File.Delete(outFile);
        }
    }
}

/// <summary>The Vulkan implementation of <see cref="IShader"/>: passes loaded from one shader json.</summary>
internal sealed class VulkanShader : IShader
{
    private readonly VulkanContext ctx;
    private readonly DescriptorSetLayout set0Layout;
    private readonly DescriptorSetLayout bindlessLayout;
    private readonly DescriptorSetLayout gameSet3Layout;
    private readonly string shaderFile;
    private ShaderData shaderData = null!;

    private VulkanShaderPass forwardPass = null!;
    private VulkanShaderPass? depthPass;
    private VulkanShaderPass? shadowPass;
    private Dictionary<string, ShaderVariableType> uniformTypes = new();

    public IShaderPass ForwardPass => forwardPass;
    public IShaderPass? DepthPass => depthPass;
    public IShaderPass? ShadowPass => shadowPass;
    public string ShaderFile => shaderFile;
    public IReadOnlyDictionary<string, ShaderVariableType> Uniforms => uniformTypes;

    internal VulkanShader(VulkanContext ctx, DescriptorSetLayout set0Layout, DescriptorSetLayout bindlessLayout, DescriptorSetLayout gameSet3Layout, string shaderFile)
    {
        this.ctx = ctx;
        this.set0Layout = set0Layout;
        this.bindlessLayout = bindlessLayout;
        this.gameSet3Layout = gameSet3Layout;
        this.shaderFile = shaderFile;
        Recompile();
    }

    public void Recompile()
    {
        forwardPass?.Dispose();
        depthPass?.Dispose();
        shadowPass?.Dispose();

        var shaderContent = File.ReadAllText(shaderFile);
        shaderData = JsonConvert.DeserializeObject<ShaderData>(shaderContent)
                     ?? throw new Exception("Failed to deserialize shader data from " + shaderFile);

        forwardPass = new VulkanShaderPass(ctx, set0Layout, bindlessLayout, gameSet3Layout, shaderData, shaderFile, false, "FORWARD_PASS");

        var pixelShader = File.ReadAllText(shaderData.Pixel.Path);
        // dedicated depth/shadow-pass variant (alpha-cutoff only, no color outputs) when the shader
        // opts in via #ifdef DEPTH_PASS; otherwise the depth/shadow passes fall back to the forward pass.
        if (pixelShader.Contains("#ifdef DEPTH_PASS"))
        {
            depthPass = new VulkanShaderPass(ctx, set0Layout, bindlessLayout, gameSet3Layout, shaderData, shaderFile, false, "DEPTH_PASS");
        }
        if (pixelShader.Contains("#ifdef SHADOW_PASS"))
        {
            shadowPass = new VulkanShaderPass(ctx, set0Layout, bindlessLayout, gameSet3Layout, shaderData, shaderFile, false, "SHADOW_PASS");
        }

        uniformTypes = new Dictionary<string, ShaderVariableType>();
        foreach (var pass in new[] { forwardPass, depthPass, shadowPass })
        {
            if (pass == null)
                continue;
            foreach (var (name, type) in pass.UniformTypes)
                uniformTypes.TryAdd(name, type);
        }
    }

    public void Dispose()
    {
        forwardPass.Dispose();
        depthPass?.Dispose();
        shadowPass?.Dispose();
    }
}
