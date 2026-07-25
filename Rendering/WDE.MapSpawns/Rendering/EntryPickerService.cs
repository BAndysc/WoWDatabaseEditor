using System;
using System.Threading.Tasks;
using Hexa.NET.ImGui;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// The "..." pick button next to editable creature/gameobject ENTRY fields in the in-view
/// inspectors. Clicking opens the app's full parameter picker dialog (CreatureParameter /
/// GameobjectParameter - the same searchable list the table editors use). The dialog is an
/// Avalonia window, so the call hops to the UI thread via <see cref="IMainThread"/>; the await
/// then resumes on the game synchronization context, so the <c>onPicked</c> callback mutates
/// editor state safely back on the game thread (a frame or more later - never during the
/// current ImGui pass).
/// </summary>
public class EntryPickerService
{
    private readonly IMainThread mainThread;
    private readonly IParameterPickerService parameterPicker;

    private bool pickInFlight;

    public EntryPickerService(IMainThread mainThread, IParameterPickerService parameterPicker)
    {
        this.mainThread = mainThread;
        this.parameterPicker = parameterPicker;
    }

    /// <summary>Draws the "..." button (same line as the preceding item). <paramref name="id"/>
    /// must be unique within the current ImGui ID scope.</summary>
    public void PickButton(string id, bool isCreature, long currentValue, Action<uint> onPicked) =>
        PickButton(id, isCreature ? "CreatureParameter" : "GameobjectParameter",
            $"Pick a {(isCreature ? "creature" : "gameobject")} from the list", currentValue, onPicked);

    /// <summary>Same button for any app-side parameter (e.g. "SpellParameter").</summary>
    public void PickButton(string id, string parameterKey, string tooltip, long currentValue, Action<uint> onPicked)
    {
        ImGui.SameLine(0, 2);
        ImGui.PushID(id);
        ImGui.BeginDisabled(pickInFlight);
        if (ImGui.SmallButton("..."u8))
            Pick(parameterKey, currentValue, onPicked).ListenErrors();
        ImGui.EndDisabled();
        ImGui.PopID();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(tooltip);
    }

    private async Task Pick(string parameterKey, long currentValue, Action<uint> onPicked)
    {
        pickInFlight = true; // one dialog at a time - a second click would stack another modal
        try
        {
            var (value, ok) = await mainThread.Schedule(() =>
                parameterPicker.PickParameter(parameterKey, currentValue));
            if (ok && value > 0)
                onPicked((uint)value);
        }
        finally
        {
            pickInFlight = false;
        }
    }
}
