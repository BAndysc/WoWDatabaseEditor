using TheEngine.Components;
using TheEngine.Interfaces;
using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

internal class RenderEnabledBitInspector : IRefInspectorDrawer<RenderEnabledBit>
{
    private readonly Engine engine;

    public RenderEnabledBitInspector(Engine engine)
    {
        this.engine = engine;
    }

    public void Draw(Entity entity, ref RenderEnabledBit component)
    {
        var isUserEnabled = !component.IsForceDisabled();

        if (ImGui.BeginTable("RenderEnabledBitTable", 2, ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Is Enabled:");
            ImGui.TableNextColumn();
            bool userEnabledValue = isUserEnabled;
            if (ImGui.Checkbox("##IsUserDisabled", ref userEnabledValue))
            {
                component.SetDisabled(!userEnabledValue);
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Render Layer:");
            ImGui.TableNextColumn();

            int currentLayer = component.Layer;
            if (currentLayer >= engine.renderManager.RenderLayers.Count)
                currentLayer = 0;

            // Get current layer name for display
            var currentLayerName = currentLayer < engine.renderManager.RenderLayers.Count ? (ReadOnlySpan<byte>)engine.renderManager.RenderLayers[currentLayer].Name : "Unknown\0"u8;

            if (ImGuiEx.BeginCombo("##RenderLayer\0"u8,currentLayerName, ImGuiComboFlags.WidthFitPreview))
            {
                for (int i = 0; i < engine.renderManager.RenderLayers.Count; i++)
                {
                    bool isSelected = (currentLayer == i);
                    if (ImGuiEx.Selectable(engine.renderManager.RenderLayers[i].Name, isSelected))
                    {
                        component.Layer = (byte)i;
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.EndTable();
        }
    }
}