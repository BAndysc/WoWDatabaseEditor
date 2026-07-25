using TheEngine;
using System.Numerics;
using Hexa.NET.ImGui;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Rendering;

/// <summary>Remembers whether the quick tour was already shown. IUserSettings is main-thread only,
/// so <see cref="SpawnEditorTutorial"/> dispatches.</summary>
public class SpawnTutorialSettings
{
    private readonly IUserSettings userSettings;

    public SpawnTutorialSettings(IUserSettings userSettings) => this.userSettings = userSettings;

    public bool LoadSeen() => userSettings.Get<Data>(new Data()).Seen;

    public void SaveSeen() => userSettings.Update(new Data { Seen = true });

    public struct Data : ISettings
    {
        public bool Seen;
    }
}

/// <summary>
/// The map-spawns quick tour: a short in-view window explaining the tools, the panels and the key
/// shortcuts. Opens by itself the first time the 3D spawn editor is used, and any time later from
/// the toolbar's "?" button.
/// </summary>
public class SpawnEditorTutorial
{
    private readonly IMainThread mainThread;
    private readonly SpawnTutorialSettings settings;

    private bool open;
    private bool firstRunHandled;
    private volatile bool seenLoaded;
    private bool seen;

    public SpawnEditorTutorial(IMainThread mainThread, SpawnTutorialSettings settings)
    {
        this.mainThread = mainThread;
        this.settings = settings;
        mainThread.Dispatch(() =>
        {
            seen = settings.LoadSeen();
            seenLoaded = true;
        });
    }

    public void Open() => open = true;

    /// <summary>Escape closes the tour like any other overlay (see <see cref="SpawnEditorKeymap"/>).</summary>
    public bool IsOpen => open;

    public void Close() => open = false;

    public void RenderGUI()
    {
        if (!firstRunHandled && seenLoaded)
        {
            firstRunHandled = true;
            if (!seen)
            {
                open = true;
                mainThread.Dispatch(() => settings.SaveSeen());
            }
        }

        if (!open)
            return;

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos + viewport.Size * 0.5f, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(560, 0), ImGuiCond.Appearing);
        if (!ImGui.Begin("Map spawns — quick tour"u8, ref open,
                ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.End();
            return;
        }

        ImGui.TextWrapped("Everything here edits the live database tables - changes stay pending " +
                          "until you Save, and the tab shows * while anything is unsaved.");

        ImGui.SeparatorText("Camera"u8);
        Bullet("W/A/S/D + right-drag: fly · E/Q: up/down · Shift: fast · wheel: zoom");
        Bullet("Wheel while right-dragging: change the fly speed · MMB: orbit · Shift+MMB: pan");
        Bullet("F: fly to the selected spawn · double-click a row in the Spawns panel: fly there");

        ImGui.SeparatorText("Tools (toolbar, hotkeys 1-9)"u8);
        Bullet("Select: click a spawn to select it · G grab · R rotate · Del delete · Ctrl+D duplicate");
        Bullet("While grabbing: X/Y/Z lock an axis · type a number for an exact move · Shift: precise");
        Bullet("Shift+A or right-click empty ground: add a creature/gameobject · double-click a spawn: edit its row");
        Bullet("Waypoints: pen tool (P) - click terrain to append points to the selected path");
        Bullet("Creature formations / Spawn groups / Pools / Graveyards / Spell targets / Creature links / Area triggers: per-core");
        Bullet("  tools - unsupported ones are greyed out; each explains itself in its right-side panel");

        ImGui.SeparatorText("Panels"u8);
        Bullet("Spawns panel (left): every map's spawns as a tree - search, right-click menus, phases");
        Bullet("Inspector (right edge): the active tool's editor for whatever is selected");
        Bullet("Hint bar (bottom): what clicks and keys do RIGHT NOW in the active tool");
        Bullet("F1: every shortcut on one page · F3: command palette (type what you want to do)");

        ImGui.SeparatorText("Saving"u8);
        Bullet("Ctrl+S or the document's save icon: saves every 3D editor at once");
        Bullet("Ctrl+Z / Ctrl+Shift+Z: undo/redo spawn edits (each panel names what it will undo)");
        Bullet("The global 'Generate query' shows the exact SQL a Save would execute");

        ImGui.Separator();
        if (ImGui.Button("Got it"u8, new Vector2(120, 0)))
            open = false;
        ImGui.SameLine();
        ImGui.TextDisabled("Reopen any time with the toolbar's ? button"u8);

        ImGui.End();
    }

    private static void Bullet(string text)
    {
        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextWrapped(text);
    }
}
