using Hexa.NET.ImGui;
using TheEngine;
using TheMaths;
using WDE.MapRenderer.Managers;
using WDE.MpqReader.DBC;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace WDE.MapRenderer.Modules;

/// <summary>
/// The view-settings strip drawn INSIDE the "3D" view, top-right corner: the area-trigger and
/// terrain-grid toggles plus a dropdown with the time &amp; display settings (everything the Avalonia
/// toolbar used to host besides the map selector). Same in-view technique as the spawn tool strip:
/// append into the engine's game-view window and report the rect via
/// <see cref="TheEngine.Managers.IEngineView.BlockClicksOver"/> so clicks never fall through into
/// the world. Lives in WDE.MapRenderer (not an editor module) so the standalone rendering tester
/// gets it too; it only talks to <see cref="IGameProperties"/>, persistence is the properties'
/// own concern.
/// </summary>
public class ViewSettingsToolbar : IGameModule
{
    private readonly Engine engine;
    private readonly IGameProperties properties;

    public object? ViewModel => null;

    private const float ButtonSize = 28f;
    private const float Margin = 10f;
    private const float BackdropPad = 6f;
    // toggle + 4 + toggle + 4 + npc-icons dropdown + separator (8 gap, line, 9 gap) + settings
    // button - fixed so the strip can be right-aligned before anything is drawn
    private const float ContentWidth = ButtonSize * 4 + 8 + 17;
    private static readonly Vector4 ActiveColor = new(0.20f, 0.45f, 0.85f, 1f);

    /// <summary>Screen-space bottom edge of the strip as of the last drawn frame (0 when it never
    /// drew). Other in-view overlays that dock top-right (the spawn inspector) stack themselves
    /// below this instead of hardcoding the strip's height.</summary>
    public static float LastStripBottom { get; private set; }

    private bool textureQualityChanged;

    public ViewSettingsToolbar(Engine engine, IGameProperties properties)
    {
        this.engine = engine;
        this.properties = properties;
    }

    public void Dispose()
    {
    }

    public void Initialize()
    {
        // push the persisted vsync wish into the engine once at startup; afterwards the
        // checkbox setter keeps the two in sync (no-op on the composition panel)
        if (engine.SupportsVSyncControl)
            engine.VSync = properties.VSync;
    }

    public void Update(float delta)
    {
    }

    public void RenderGUI()
    {
        // append into the engine's game-view window (drawn earlier this frame) - the strip lives
        // in the 3D view itself and moves/hides with it
        if (!ImGui.Begin("3D"))
        {
            ImGui.End();
            return;
        }

        var view = engine.GameView.ViewRect;

        var dl = ImGui.GetWindowDrawList();
        dl.ChannelsSplit(2);
        dl.ChannelsSetCurrent(1); // buttons first; the backdrop is drawn under them afterwards

        ImGui.SetCursorScreenPos(new Vector2(view.Right - Margin - BackdropPad - ContentWidth, view.Y + Margin + BackdropPad));

        ImGui.BeginGroup();

        if (IconButton("##show_areatriggers", Icon.AreaTrigger, properties.ShowAreaTriggers, "Show area triggers"))
            properties.ShowAreaTriggers = !properties.ShowAreaTriggers;
        ImGui.SameLine(0, 4);
        if (IconButton("##show_grid", Icon.Grid, properties.ShowGrid, "Show map chunk grid"))
            properties.ShowGrid = !properties.ShowGrid;
        ImGui.SameLine(0, 4);
        if (IconButton("##npc_status_icons", Icon.NpcIcons, properties.ShowStatusIcons, "NPC status icons (quest, gossip, AI)"))
            ImGui.OpenPopup("##status_icons_popup");
        var npcIconsMax = ImGui.GetItemRectMax();
        Utils.ImGuiIconButtons.DropdownCaret(dl, npcIconsMax, ImGui.GetColorU32(ImGuiCol.Text, 0.8f));
        DrawStatusIconsPopup(npcIconsMax);

        VerticalSeparator(dl);

        if (IconButton("##view_settings", Icon.Settings, false, "Time & display settings"))
            ImGui.OpenPopup("##view_settings_popup");
        // dropdown caret in the button's corner
        var settingsMax = ImGui.GetItemRectMax();
        Utils.ImGuiIconButtons.DropdownCaret(dl, settingsMax, ImGui.GetColorU32(ImGuiCol.Text, 0.8f));
        DrawSettingsPopup(settingsMax);

        ImGui.EndGroup();

        var stripMin = ImGui.GetItemRectMin() - new Vector2(BackdropPad, BackdropPad);
        var stripMax = ImGui.GetItemRectMax() + new Vector2(BackdropPad, BackdropPad);
        dl.ChannelsSetCurrent(0);
        Utils.ImGuiIconButtons.Backdrop(dl, stripMin, stripMax);
        dl.ChannelsMerge();

        // clicks on the strip must not fall through into the world underneath
        engine.GameView.BlockClicksOver(new RectangleF(stripMin.X, stripMin.Y, stripMax.X - stripMin.X, stripMax.Y - stripMin.Y));
        LastStripBottom = stripMax.Y;

        ImGui.End();
    }

    private void DrawStatusIconsPopup(Vector2 buttonMax)
    {
        const float popupWidth = 200f;
        // anchor under the button, right-aligned so it never overflows the view
        ImGui.SetNextWindowPos(new Vector2(buttonMax.X - popupWidth, buttonMax.Y + BackdropPad + 4));
        ImGui.SetNextWindowSize(new Vector2(popupWidth, 0));
        if (!ImGuiEx.BeginPopup("##status_icons_popup"))
            return;

        bool show = properties.ShowStatusIcons;
        if (ImGui.Checkbox("NPC status icons", ref show))
            properties.ShowStatusIcons = show;

        ImGui.Separator();
        ImGui.BeginDisabled(!show);
        foreach (var (icon, label) in StatusIconsManager.Icons)
        {
            uint bit = StatusIconsManager.BitOf(icon);
            bool visible = (properties.StatusIconsHiddenMask & bit) == 0;
            if (ImGui.Checkbox(label, ref visible))
                properties.StatusIconsHiddenMask = visible
                    ? properties.StatusIconsHiddenMask & ~bit
                    : properties.StatusIconsHiddenMask | bit;
        }
        ImGui.EndDisabled();
        ImGui.EndPopup();
    }

    private void DrawSettingsPopup(Vector2 buttonMax)
    {
        const float popupWidth = 320f;
        // anchor under the settings button, right-aligned so it never overflows the view
        ImGui.SetNextWindowPos(new Vector2(buttonMax.X - popupWidth, buttonMax.Y + BackdropPad + 4));
        ImGui.SetNextWindowSize(new Vector2(popupWidth, 0));
        if (!ImGuiEx.BeginPopup("##view_settings_popup"))
            return;

        ImGui.PushItemWidth(-115);

        ImGui.TextDisabled("Time");

        bool paused = properties.DisableTimeFlow;
        if (ImGui.Checkbox("Pause time flow", ref paused))
            properties.DisableTimeFlow = paused;

        ImGui.BeginDisabled(paused);
        int speed = properties.TimeSpeedMultiplier;
        if (ImGui.SliderInt("Time speed", ref speed, 0, 6))
            properties.TimeSpeedMultiplier = speed;
        ImGui.EndDisabled();

        int minutes = properties.CurrentTime.TotalMinutes;
        if (ImGui.SliderInt("Time", ref minutes, 0, 1439, $"{minutes / 60:00}:{minutes % 60:00}"))
            properties.CurrentTime = Time.FromMinutes(minutes);

        bool overrideLighting = properties.OverrideLighting;
        if (ImGui.Checkbox("Disable lighting", ref overrideLighting))
            properties.OverrideLighting = overrideLighting;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextDisabled("Display");

        float viewDistance = properties.ViewDistanceModifier;
        if (ImGui.SliderFloat("View distance", ref viewDistance, 1, 24, "%.1f"))
            properties.ViewDistanceModifier = viewDistance;

        float dynamicResolution = properties.DynamicResolution;
        if (ImGui.SliderFloat("Resolution scale", ref dynamicResolution, 0.1f, 1f, "%.2f"))
            properties.DynamicResolution = dynamicResolution;

        // the composition panel presents through the Avalonia compositor: always vsynced,
        // nothing to toggle - show the checkbox checked and disabled
        bool canControlVSync = engine.SupportsVSyncControl;
        bool vsync = !canControlVSync || properties.VSync;
        ImGui.BeginDisabled(!canControlVSync);
        if (ImGui.Checkbox("VSync", ref vsync))
        {
            properties.VSync = vsync;
            engine.VSync = vsync;
        }
        ImGui.EndDisabled();
        if (!canControlVSync && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("The composition 3D panel is paced by the Avalonia compositor,\nvsync can't be turned off there (switch the panel type in the settings)");

        int quality = properties.TextureQuality;
        if (ImGui.SliderInt("Texture quality", ref quality, 0, 6))
        {
            properties.TextureQuality = quality;
            textureQualityChanged = true;
        }
        if (textureQualityChanged)
            ImGui.TextDisabled("Restart the game view to apply the change");

        ImGui.PopItemWidth();
        ImGui.EndPopup();
    }

    private void VerticalSeparator(ImDrawListPtr dl)
    {
        ImGui.SameLine(0, 8);
        var pos = ImGui.GetCursorScreenPos();
        dl.AddLine(new Vector2(pos.X, pos.Y + 4), new Vector2(pos.X, pos.Y + ButtonSize - 4), ImGui.GetColorU32(ImGuiCol.Border), 1f);
        ImGui.SameLine(0, 8 + 1);
    }

    private enum Icon
    {
        AreaTrigger,
        Grid,
        NpcIcons,
        Settings,
    }

    /// <summary>An icon button - shared mechanics (see <see cref="Utils.ImGuiIconButtons"/>),
    /// this strip's own glyphs.</summary>
    private static bool IconButton(string id, Icon icon, bool active, string tooltip)
    {
        var slot = Utils.ImGuiIconButtons.IconButton(id, ButtonSize, active, tooltip, true, ImGui.GetColorU32(ActiveColor));
        DrawIcon(ImGui.GetWindowDrawList(), icon, slot.GlyphOrigin, slot.GlyphSize, slot.GlyphColor);
        return slot.Clicked;
    }

    private static void DrawIcon(ImDrawListPtr dl, Icon icon, Vector2 origin, float s, uint col)
    {
        float th = Utils.ImGuiIconButtons.Stroke(s);
        Vector2 P(float x, float y) => origin + new Vector2(x, y) * s;

        switch (icon)
        {
            case Icon.AreaTrigger:
            {
                // trigger radius: dashed ring with a filled center dot
                var c = P(0.5f, 0.5f);
                float r = s * 0.42f;
                for (int i = 0; i < 4; ++i)
                {
                    float a0 = i * MathF.PI / 2 + 0.28f;
                    float a1 = a0 + MathF.PI / 2 - 0.56f;
                    dl.PathArcTo(c, r, a0, a1, 8);
                    dl.PathStroke(col, ImDrawFlags.None, th);
                }
                dl.AddCircleFilled(c, s * 0.14f, col);
                break;
            }
            case Icon.Grid:
            {
                // 3x3 map chunk grid
                var min = P(0.05f, 0.05f);
                var max = P(0.95f, 0.95f);
                dl.AddRect(min, max, col, 0, 0, th);
                dl.AddLine(P(0.35f, 0.05f), P(0.35f, 0.95f), col, th * 0.8f);
                dl.AddLine(P(0.65f, 0.05f), P(0.65f, 0.95f), col, th * 0.8f);
                dl.AddLine(P(0.05f, 0.35f), P(0.95f, 0.35f), col, th * 0.8f);
                dl.AddLine(P(0.05f, 0.65f), P(0.95f, 0.65f), col, th * 0.8f);
                break;
            }
            case Icon.NpcIcons:
            {
                // a quest-mark "!" - the archetypal NPC status icon
                dl.AddLine(P(0.5f, 0.06f), P(0.5f, 0.60f), col, th * 1.8f);
                dl.AddCircleFilled(P(0.5f, 0.88f), s * 0.11f, col);
                break;
            }
            case Icon.Settings:
            {
                // mixer sliders: three tracks with knobs at different positions
                dl.AddLine(P(0.02f, 0.20f), P(0.98f, 0.20f), col, th);
                dl.AddLine(P(0.02f, 0.50f), P(0.98f, 0.50f), col, th);
                dl.AddLine(P(0.02f, 0.80f), P(0.98f, 0.80f), col, th);
                dl.AddCircleFilled(P(0.32f, 0.20f), s * 0.12f, col);
                dl.AddCircleFilled(P(0.72f, 0.50f), s * 0.12f, col);
                dl.AddCircleFilled(P(0.48f, 0.80f), s * 0.12f, col);
                break;
            }
        }
    }
}
