using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class MeshRendererInspector : IRefInspectorDrawer<MeshRenderer>
{
    private readonly Engine engine;

    public MeshRendererInspector(Engine engine)
    {
        this.engine = engine;
    }

    public unsafe void Draw(Entity entity, ref MeshRenderer component)
    {
        var mesh = engine.meshManager.GetMeshByHandle(component.MeshHandle);
        var material = engine.materialManager.GetMaterialByHandle(component.MaterialHandle);

        ImGui.Columns(2);
        ImGuiEx.TextUnformatted("Submesh\0"u8);
        ImGui.NextColumn();
        // SubMeshId/Opaque are properties (they recompute MeshRenderer.SortKey on assignment), so they
        // can't be passed by ref - edit a local and write back through the property when it changes.
        int subMeshId = component.SubMeshId;
        if (ImGui.SliderInt("##submesh", ref subMeshId, 0, mesh.SubmeshCount - 1))
            component.SubMeshId = subMeshId;
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Opaque\0"u8);
        ImGui.NextColumn();
        bool opaque = component.Opaque;
        if (ImGui.Checkbox("##opaque", ref opaque))
            component.Opaque = opaque;
        ImGui.NextColumn();

        // ImGuiEx.TextUnformatted("Blending enabled\0"u8);
        // ImGui.NextColumn();
        // ImGui.Checkbox("##blending_enabled", ref material.BlendingEnabled);
        // ImGui.NextColumn();
        //
        // ImGuiEx.TextUnformatted("Z-Write\0"u8);
        // ImGui.NextColumn();
        // ImGui.Checkbox("##z_write", ref material.ZWrite);
        // ImGui.NextColumn();

        // Shader file display
        ImGuiEx.TextUnformatted("Shader\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted(material.Pipeline.Shader.ShaderFile);
        ImGui.NextColumn();

        // Culling mode combo box
        // ImGuiEx.TextUnformatted("Culling\0"u8);
        // ImGui.NextColumn();
        // if (ImGui.BeginCombo("##culling", material.Culling.ToString()))
        // {
        //     var cullingValues = new[] { CullingMode.Front, CullingMode.Back, CullingMode.Off };
        //     foreach (var culling in cullingValues)
        //     {
        //         bool isSelected = material.Culling == culling;
        //         if (ImGui.Selectable(culling.ToString(), isSelected))
        //         {
        //             material.Culling = culling;
        //         }
        //         if (isSelected)
        //             ImGui.SetItemDefaultFocus();
        //     }
        //     ImGui.EndCombo();
        // }
        // ImGui.NextColumn();

        // Depth testing combo box
        // ImGuiEx.TextUnformatted("Depth Testing\0"u8);
        // ImGui.NextColumn();
        // if (ImGui.BeginCombo("##depth_testing", material.DepthTesting.ToString()))
        // {
        //     var depthValues = new[] {
        //         DepthCompare.Never, DepthCompare.Less, DepthCompare.Equal, DepthCompare.Lequal,
        //         DepthCompare.Greater, DepthCompare.Notequal, DepthCompare.Gequal, DepthCompare.Always
        //     };
        //     foreach (var depth in depthValues)
        //     {
        //         bool isSelected = material.DepthTesting == depth;
        //         if (ImGui.Selectable(depth.ToString(), isSelected))
        //         {
        //             material.DepthTesting = depth;
        //         }
        //         if (isSelected)
        //             ImGui.SetItemDefaultFocus();
        //     }
        //     ImGui.EndCombo();
        // }
        // ImGui.NextColumn();

        // Destination blending combo box
        // ImGuiEx.TextUnformatted("Destination Blending\0"u8);
        // ImGui.NextColumn();
        // if (ImGui.BeginCombo("##destination_blending", material.DestinationBlending.ToString()))
        // {
        //     var blendingValues = new[] {
        //         Blending.Zero, Blending.One, Blending.SrcColor, Blending.OneMinusSrcColor,
        //         Blending.SrcAlpha, Blending.OneMinusSrcAlpha, Blending.DstAlpha, Blending.OneMinusDstAlpha,
        //         Blending.DstColor, Blending.OneMinusDstColor, Blending.SrcAlphaSaturate, Blending.ConstantColor,
        //         Blending.OneMinusConstantColor, Blending.ConstantAlpha, Blending.OneMinusConstantAlpha,
        //         Blending.Src1Alpha, Blending.Src1Color, Blending.OneMinusSrc1Color, Blending.OneMinusSrc1Alpha
        //     };
        //     foreach (var blending in blendingValues)
        //     {
        //         bool isSelected = material.DestinationBlending == blending;
        //         if (ImGui.Selectable(blending.ToString(), isSelected))
        //         {
        //             material.DestinationBlending = blending;
        //         }
        //         if (isSelected)
        //             ImGui.SetItemDefaultFocus();
        //     }
        //     ImGui.EndCombo();
        // }
        // ImGui.NextColumn();

        // Display all uniforms
        if (material.thisUniformData != null)
        {
            var bytes = material.MaterialDataBytes;
            foreach (var uniform in material.thisUniformData)
            {
                var slotBytes = bytes.Slice((int)uniform.offset, uniform.size);
                ImGui.TextUnformatted($"{uniform.name} ({uniform.type})");
                ImGui.NextColumn();
                fixed (byte* ptr = slotBytes)
                {
                    if (uniform.type == typeof(int))
                    {
                        ref var value = ref Unsafe.AsRef<int>(ptr);
                        ImGui.InputInt($"##{uniform.name}_int", ref value);
                    }
                    else if (uniform.type == typeof(float))
                    {
                        ref var value = ref Unsafe.AsRef<float>(ptr);
                        ImGui.InputFloat($"##{uniform.name}_float", ref value);
                    }
                    else if (uniform.type == typeof(Vector3))
                    {
                        ref var value = ref Unsafe.AsRef<Vector3>(ptr);
                        ImGui.InputFloat3($"##{uniform.name}_float3", ref value);
                    }
                    else if (uniform.type == typeof(Vector4))
                    {
                        ref var value = ref Unsafe.AsRef<Vector4>(ptr);
                        ImGui.InputFloat4($"##{uniform.name}_float4", ref value);
                    }
                    else if (uniform.type == typeof(Matrix))
                    {
                        ref var value = ref Unsafe.AsRef<Matrix>(ptr);
                        ImGui.TextUnformatted("Matrix (read-only)");
                    }
                    else if (uniform.type == typeof(BindlessTextureId))
                    {
                        // binary-compatible with an int; the value is a bindless slot, so preview the
                        // texture it points at instead of showing a meaningless number.
                        ref var slot = ref Unsafe.AsRef<int>(ptr);
                        DrawBindlessTexture(slot);
                    }
                    else
                        throw new Exception("Unknown type " + uniform.type);
                    ImGui.NextColumn();
                }
            }
        }

        // (textures are bindless now - they appear above as int "...Index" fields in the material data,
        // not as set-1 sampler uniforms, so there's no per-material texture list to preview here)

        ImGui.Columns(1);


        if (ImGui.Button("Save to obj"))
        {
            mesh.SaveToObj("mesh.obj");
        }
    }

    private unsafe void DrawBindlessTexture(int slot)
    {
        var texture = engine.textureManager.TryGetTextureByBindlessIndex(slot);
        if (texture == null)
        {
            ImGui.TextUnformatted($"bindless slot {slot} (no preview)");
            return;
        }

        var texRef = new ImTextureRef(null, texture.Handle.ToRawIntPtr());
        // native top-down render targets: no V-flip (uv0 top-left, uv1 bottom-right)
        ImGui.Image(texRef, new Vector2(48, 48), new Vector2(0, 0), new Vector2(1, 1));
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted($"slot {slot} - {texture.Width}x{texture.Height}");
            ImGui.Image(texRef, new Vector2(256, 256), new Vector2(0, 0), new Vector2(1, 1));
            ImGui.EndTooltip();
        }
        ImGui.SameLine();
        ImGui.TextUnformatted($"slot {slot}");
    }
}