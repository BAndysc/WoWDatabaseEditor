using Hexa.NET.ImGui;
using TheEngine.Interfaces;
using TheEngine.Vulkan;

namespace TheEngine.Windows;

public static class TexturePickerWindow
{
    private static string filter = string.Empty;
    private static ITexture? current;

    private static bool hasPendingResult;
    private static ITexture? pendingResult;
    private static string pendingId = string.Empty;

    /// <summary>
    /// Draws an inline thumbnail button for <paramref name="texture"/>; clicking it opens a
    /// popup (drawn right here, for as long as the caller keeps calling Field with this same id).
    /// Returns true (and updates <paramref name="texture"/>) the first call after a new texture
    /// was picked from that popup.
    /// </summary>
    public static unsafe bool Field(Engine engine, string id, ref ITexture? texture)
    {
        bool changed = false;
        if (hasPendingResult && pendingId == id)
        {
            texture = pendingResult;
            hasPendingResult = false;
            changed = true;
        }

        ImGui.PushID(id);

        const float size = 32f;
        if (texture != null)
        {
            float aspect = (float)texture.Width / texture.Height;
            float w = aspect > 1f ? size : size * aspect;
            float h = aspect > 1f ? size / aspect : size;
            ImGui.Image(new ImTextureRef(null, texture.Handle.ToRawIntPtr()), new Vector2(w, h));

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Image(new ImTextureRef(null, texture.Handle.ToRawIntPtr()), new Vector2(texture.Width, texture.Height));
                ImGui.EndTooltip();
            }

            ImGui.SameLine();
        }

        if (ImGui.Button("Pick..."u8))
        {
            current = texture;
            filter = string.Empty;
            ImGui.OpenPopup("texture_picker_popup"u8);
        }

        DrawPickerPopup(engine, id);

        ImGui.PopID();
        return changed;
    }

    private static unsafe void DrawPickerPopup(Engine engine, string id)
    {
        // Fixed size/position, centered, forced every frame (Always, not FirstUseEver) - no
        // drag, no resize, so there's nothing for the user to fight or for stale ImGui-remembered
        // window state to mess up.
        var display = ImGui.GetIO().DisplaySize / ImGui.GetIO().DisplayFramebufferScale;
        var size = new Vector2(display.X * 0.75f, display.Y * 0.5f);
        var pos = (display - size) * 0.5f;
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
        if (!ImGui.BeginPopup("texture_picker_popup"u8, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
            return;

        ImGui.InputText("##texture_picker_filter"u8, ref filter, 128);
        ImGui.Separator();

        ITexture? picked = null;
        bool didPick = false;

        if (ImGui.Selectable("(none)"u8, current == null))
            didPick = true;
        ImGui.Separator();

        ImGui.BeginChild("##texture_picker_grid"u8, new Vector2(0, 0));

        const float thumbSize = 64f;
        const float cellSize = thumbSize + 16f;
        float avail = ImGui.GetContentRegionAvail().X;
        int columns = Math.Max(1, (int)(avail / cellSize));
        int column = 0;

        foreach (var tex in engine.textureManager.AllTextures)
        {
            // render targets (backbuffers, depth prepass aliases, the scene view's color/depth
            // textures, etc.) live in the same texture list but aren't sampleable assets - a
            // depth-only one (zero color attachments) crashes VulkanRenderBackend.GetBindlessTextureSlot's
            // rt.Colors[0] when ImGui later tries to bindless-sample it for the thumbnail.
            if (tex.NativeTexture is VulkanRenderTexture)
                continue;

            string name = engine.textureManager.GetPathFor(tex) is { } path ? Path.GetFileName(path) : tex.Handle.ToString();
            if (filter.Length > 0 && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            ImGui.PushID(name + tex.Handle);
            ImGui.BeginGroup();
            // captured before the centering offset below shifts the cursor, so the text wrap
            // boundary always spans the full cell width regardless of where in the row it is -
            // ImGui.TextWrapped() otherwise wraps to the window's remaining width from the
            // current cursor, which shrinks to almost nothing for the rightmost column.
            float cellStartX = ImGui.GetCursorPosX();

            float aspect = (float)tex.Width / tex.Height;
            float w = aspect > 1f ? thumbSize : thumbSize * aspect;
            float h = aspect > 1f ? thumbSize / aspect : thumbSize;
            ImGui.SetCursorPosX(cellStartX + (thumbSize - w) * 0.5f);
            ImGui.Image(new ImTextureRef(null, tex.Handle.ToRawIntPtr()), new Vector2(w, h));
            bool clicked = ImGui.IsItemClicked();
            if (tex == current)
            {
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                ImGui.GetWindowDrawList().AddRect(min, max, ImGui.GetColorU32(ImGuiCol.PlotHistogram));
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted($"{name} ({tex.Width}x{tex.Height})");
                ImGui.EndTooltip();
            }

            ImGui.SetCursorPosX(cellStartX);
            ImGui.PushTextWrapPos(cellStartX + thumbSize);
            ImGui.TextWrapped(name);
            ImGui.PopTextWrapPos();

            ImGui.EndGroup();
            ImGui.PopID();

            if (clicked)
            {
                picked = tex;
                didPick = true;
            }

            column++;
            if (column < columns)
                ImGui.SameLine(0, 16);
            else
                column = 0;
        }

        ImGui.EndChild();

        if (didPick)
        {
            pendingResult = picked;
            pendingId = id;
            hasPendingResult = true;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }
}
