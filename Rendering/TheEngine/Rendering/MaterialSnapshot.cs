using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Interfaces;

namespace TheEngine.Rendering;

/// <summary>
/// A record-time copy of everything <see cref="Material.ActivateUniforms(ShaderPass, MaterialInstanceRenderData?)"/>
/// reads: the material's constant data, its textures and structured buffers, with the
/// instance-data overrides already applied and the texture slots already assigned.
///
/// Materials are mutable and routinely changed between draws (ImGui swaps the font texture,
/// postprocesses swap their input, debug shapes change color), so a deferred command list
/// captures this snapshot when the bind is recorded and replays from it - the Vulkan
/// equivalent of writing a descriptor set and push constants at record time.
///
/// Instances are pooled and reused by the recording command list - capturing allocates
/// nothing in steady state.
/// </summary>
public sealed class MaterialSnapshot
{
    // slots are pre-resolved here so the replay does no dictionary walks or shader queries
    internal readonly List<(GlobalUniformHandle uniform, int slot, INativeBuffer buffer)> Buffers = new();
    internal readonly List<(GlobalUniformHandle uniform, int slot, ITexture? texture)> Textures = new();
    internal byte[] MaterialData = Array.Empty<byte>();
    internal int MaterialDataLength;

    internal void CaptureFrom(Material material, IShaderPass shaderPass, MaterialInstanceRenderData? instanceData)
    {
        Clear();
        // mirrors the slot walk of the live activation path exactly, so the replayed
        // binds land in the same texture units the shader was set up for
        int slot = 0;
        foreach (var buffer in material.structuredBuffers)
        {
            if (instanceData?.structuredBuffers != null && instanceData.structuredBuffers.ContainsKey(buffer.Key))
                continue;
            if (!shaderPass.HasGlobalUniform(buffer.Key))
                continue;
            Buffers.Add((buffer.Key, slot++, buffer.Value));
        }
        foreach (var pair in material.textures)
        {
            if (!shaderPass.HasGlobalUniform(pair.Key))
                continue;
            Textures.Add((pair.Key, slot++, pair.Value));
        }
        var data = material.MaterialDataBytes;
        if (MaterialData.Length < data.Length)
            MaterialData = new byte[data.Length];
        data.CopyTo(MaterialData);
        MaterialDataLength = data.Length;
        if (instanceData?.structuredBuffers != null)
        {
            foreach (var buffer in instanceData.structuredBuffers)
            {
                if (!shaderPass.HasGlobalUniform(buffer.Key))
                    continue;
                Buffers.Add((buffer.Key, slot++, buffer.Value));
            }
        }
    }

    internal void Clear()
    {
        Buffers.Clear();
        Textures.Clear();
        MaterialDataLength = 0;
    }
}
