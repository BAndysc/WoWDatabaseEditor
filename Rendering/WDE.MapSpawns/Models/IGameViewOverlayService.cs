using System;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Models;

/// <summary>A tool's contribution to the in-view inspector panel and the bottom hint bar. One
/// section per <see cref="SpawnEditorTool"/>, registered by the tool's game module (engine thread);
/// the game-view inspector draws the ACTIVE tool's section each GUI frame.</summary>
public interface IInspectorSection
{
    /// <summary>Panel header, e.g. "Waypoints".</summary>
    string Title { get; }

    /// <summary>True when this tool has unsaved changes - the inspector shows an "unsaved" badge
    /// next to the title (and a dot on the collapsed tab). Re-read every frame.</summary>
    bool IsDirty => false;

    /// <summary>Saves only this editor's pending changes (the toolbar Save still saves everything).
    /// Return null to hide the header Save button (e.g. nothing to save right now).</summary>
    Func<Task>? SaveSelf => null;

    /// <summary>True when <see cref="SaveSelf"/> reports its own outcome (toast) - the inspector
    /// then skips its generic "{Title} saved" toast (it would fire before an event-driven save ran).</summary>
    bool SaveReportsItself => false;

    /// <summary>Discards this editor's unsaved changes by reloading from the database. Return null
    /// to hide the header Revert button. The inspector confirms before calling it.</summary>
    Func<Task>? RevertSelf => null;

    /// <summary>What <see cref="RevertSelf"/> actually throws away, spelled out in the confirm
    /// dialog ("Discard {RevertScope}?"). Most tools reload the whole map's data, so the default
    /// says so; a tool with a narrower revert (waypoints: only the open path) overrides this.</summary>
    string RevertScope => $"ALL unsaved {Title} changes on this map";

    /// <summary>One-line keymap/status shown in the hint bar at the bottom of the 3D view.
    /// Re-read every frame, so it can react to the current state; null hides the bar.</summary>
    string? Hints { get; }

    /// <summary>Draws the panel body (called inside the inspector child window).</summary>
    void DrawContent();
}

/// <summary>Connects the tool modules to the single in-view inspector: each module registers its
/// section in Initialize and unregisters it in Dispose (modules are game-scoped, this service is
/// not - a stale section would outlive its game otherwise).</summary>
[UniqueProvider]
public interface IGameViewOverlayService
{
    void SetSection(SpawnEditorTool tool, IInspectorSection? section);
    IInspectorSection? GetSection(SpawnEditorTool tool);
}

public class GameViewOverlayService : IGameViewOverlayService
{
    private readonly IInspectorSection?[] sections = new IInspectorSection?[Enum.GetValues<SpawnEditorTool>().Length];

    public void SetSection(SpawnEditorTool tool, IInspectorSection? section) => sections[(int)tool] = section;

    public IInspectorSection? GetSection(SpawnEditorTool tool) => sections[(int)tool];
}
