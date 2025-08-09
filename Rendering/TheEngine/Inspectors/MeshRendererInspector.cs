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

    public void Draw(Entity entity, ref MeshRenderer component)
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

        ImGuiEx.TextUnformatted("Blending enabled\0"u8);
        ImGui.NextColumn();
        ImGui.Checkbox("##blending_enabled", ref material.BlendingEnabled);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Z-Write\0"u8);
        ImGui.NextColumn();
        ImGui.Checkbox("##z_write", ref material.ZWrite);
        ImGui.NextColumn();

        // Shader file display
        ImGuiEx.TextUnformatted("Shader\0"u8);
        ImGui.NextColumn();
        ImGui.TextUnformatted(material.Shader.ShaderFile);
        ImGui.NextColumn();

        // Culling mode combo box
        ImGuiEx.TextUnformatted("Culling\0"u8);
        ImGui.NextColumn();
        if (ImGui.BeginCombo("##culling", material.Culling.ToString()))
        {
            var cullingValues = new[] { CullingMode.Front, CullingMode.Back, CullingMode.Off };
            foreach (var culling in cullingValues)
            {
                bool isSelected = material.Culling == culling;
                if (ImGui.Selectable(culling.ToString(), isSelected))
                {
                    material.Culling = culling;
                }
                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        // Depth testing combo box
        ImGuiEx.TextUnformatted("Depth Testing\0"u8);
        ImGui.NextColumn();
        if (ImGui.BeginCombo("##depth_testing", material.DepthTesting.ToString()))
        {
            var depthValues = new[] {
                DepthCompare.Never, DepthCompare.Less, DepthCompare.Equal, DepthCompare.Lequal,
                DepthCompare.Greater, DepthCompare.Notequal, DepthCompare.Gequal, DepthCompare.Always
            };
            foreach (var depth in depthValues)
            {
                bool isSelected = material.DepthTesting == depth;
                if (ImGui.Selectable(depth.ToString(), isSelected))
                {
                    material.DepthTesting = depth;
                }
                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        // Destination blending combo box
        ImGuiEx.TextUnformatted("Destination Blending\0"u8);
        ImGui.NextColumn();
        if (ImGui.BeginCombo("##destination_blending", material.DestinationBlending.ToString()))
        {
            var blendingValues = new[] {
                Blending.Zero, Blending.One, Blending.SrcColor, Blending.OneMinusSrcColor,
                Blending.SrcAlpha, Blending.OneMinusSrcAlpha, Blending.DstAlpha, Blending.OneMinusDstAlpha,
                Blending.DstColor, Blending.OneMinusDstColor, Blending.SrcAlphaSaturate, Blending.ConstantColor,
                Blending.OneMinusConstantColor, Blending.ConstantAlpha, Blending.OneMinusConstantAlpha,
                Blending.Src1Alpha, Blending.Src1Color, Blending.OneMinusSrc1Color, Blending.OneMinusSrc1Alpha
            };
            foreach (var blending in blendingValues)
            {
                bool isSelected = material.DestinationBlending == blending;
                if (ImGui.Selectable(blending.ToString(), isSelected))
                {
                    material.DestinationBlending = blending;
                }
                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        // Display all uniforms
        // Int uniforms
        foreach (var uniform in material.intUniforms)
        {
            var uniformName = material.Shader.GetUniformName(uniform.Key);
            if (uniformName != null)
            {
                ImGui.TextUnformatted($"{uniformName} (int)");
                ImGui.NextColumn();
                var value = uniform.Value;
                if (ImGui.InputInt($"##{uniformName}_int", ref value))
                {
                    material.SetUniformInt(uniformName, value);
                }
                ImGui.NextColumn();
            }
        }

        // Float uniforms
        foreach (var uniform in material.floatUniforms)
        {
            var uniformName = material.Shader.GetUniformName(uniform.Key);
            if (uniformName != null)
            {
                ImGui.TextUnformatted($"{uniformName} (float)");
                ImGui.NextColumn();
                var value = uniform.Value;
                if (ImGui.InputFloat($"##{uniformName}_float", ref value))
                {
                    material.SetUniform(uniformName, value);
                }
                ImGui.NextColumn();
            }
        }

        // Vector3 uniforms
        foreach (var uniform in material.vector3Uniforms)
        {
            var uniformName = material.Shader.GetUniformName(uniform.Key);
            if (uniformName != null)
            {
                ImGui.TextUnformatted($"{uniformName} (vec3)");
                ImGui.NextColumn();
                var value = uniform.Value;
                if (ImGui.InputFloat3($"##{uniformName}_vec3", ref value))
                {
                    material.SetUniform(uniformName, value);
                }
                ImGui.NextColumn();
            }
        }

        // Vector4 uniforms
        foreach (var uniform in material.vector4Uniforms)
        {
            var uniformName = material.Shader.GetUniformName(uniform.Key);
            if (uniformName != null)
            {
                ImGui.TextUnformatted($"{uniformName} (vec4)");
                ImGui.NextColumn();
                var value = uniform.Value;
                if (ImGui.InputFloat4($"##{uniformName}_vec4", ref value))
                {
                    material.SetUniform(uniformName, value);
                }
                ImGui.NextColumn();
            }
        }

        // Matrix uniforms (display as read-only for now due to complexity)
        foreach (var uniform in material.matrixUniforms)
        {
            var uniformName = material.Shader.GetUniformName(uniform.Key);
            if (uniformName != null)
            {
                ImGui.TextUnformatted($"{uniformName} (matrix)");
                ImGui.NextColumn();
                ImGui.TextUnformatted("Matrix (read-only)");
                ImGui.NextColumn();
            }
        }

        // Texture uniforms
        foreach (var textureUniform in material.textures)
        {
            var uniformName = material.Shader.GetUniformName(textureUniform.Key);
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