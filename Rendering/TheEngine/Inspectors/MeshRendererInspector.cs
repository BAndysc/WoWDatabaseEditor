using System.Runtime.CompilerServices;
using ImGuiNET;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
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
        ImGui.SliderInt("##submesh", ref component.SubMeshId, 0, mesh.SubmeshCount - 1);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Opaque\0"u8);
        ImGui.NextColumn();
        ImGui.Checkbox("##opaque", ref component.Opaque);
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
                    else
                        throw new Exception("Unknown type " + uniform.type);
                    ImGui.NextColumn();
                }
            }
        }

        // Texture uniforms
        foreach (var textureUniform in material.textures)
        {
            var uniformName = Material.GetUniformName(textureUniform.Key);
            if (uniformName != null)
            {
                var texture = textureUniform.Value;
                if (texture != null)
                {
                    ImGui.TextUnformatted($"{uniformName} (texture)");
                    ImGui.NextColumn();

                    // Calculate thumbnail size (max 50px, keep aspect ratio)
                    var maxSize = 50.0f;
                    var aspectRatio = (float)texture.Width / texture.Height;
                    var thumbnailWidth = aspectRatio > 1.0f ? maxSize : maxSize * aspectRatio;
                    var thumbnailHeight = aspectRatio > 1.0f ? maxSize / aspectRatio : maxSize;

                    var texturePtr = texture.Handle.ToRawIntPtr();
                    ImGui.Image(texturePtr, new Vector2(thumbnailWidth, thumbnailHeight));

                    // Show full resolution on hover
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Image(texturePtr, new Vector2(texture.Width, texture.Height));
                        ImGui.EndTooltip();
                    }

                    ImGui.NextColumn();
                }
                else
                {
                    ImGui.TextUnformatted($"{uniformName} (texture)");
                    ImGui.NextColumn();
                    ImGui.TextUnformatted("Texture not found");
                    ImGui.NextColumn();
                }
            }
        }

        ImGui.Columns(1);


        if (ImGui.Button("Save to obj"))
        {
            mesh.SaveToObj("mesh.obj");
        }
    }
}