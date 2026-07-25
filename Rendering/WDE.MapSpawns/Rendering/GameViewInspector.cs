using System.Numerics;
using Hexa.NET.ImGui;
using TheEngine;
using TheMaths;
using WDE.MapSpawns.Models;
using Vector2 = System.Numerics.Vector2;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// The contextual in-view inspector: a collapsible panel docked to the right edge of the "3D"
/// window plus a one-line hint bar at the bottom. There are no floating editor windows - the
/// active tool's <see cref="IInspectorSection"/> (from <see cref="IGameViewOverlayService"/>)
/// provides both the panel body and the hint line. Rendered from <see cref="SpawnViewer.RenderGUI"/>,
/// same in-view technique as <see cref="GameViewToolbar"/> (append to the engine's window,
/// <see cref="TheEngine.Managers.IEngineView.BlockClicksOver"/> so clicks never fall through).
/// </summary>
public class GameViewInspector
{
    private readonly Engine engine;
    private readonly ISpawnEditorToolService toolService;
    private readonly IGameViewOverlayService overlays;
    private readonly IGameNotificationService notifications;
    private readonly WDE.Common.Tasks.IMainThread mainThread;
    private readonly SpawnInspectorSettings settings;

    private bool collapsed;
    private float panelWidth = DefaultPanelWidth;
    private Task? sectionOpTask; // an in-flight per-editor save/revert; gates the header buttons
    private bool openRevertPopup;

    // persisted chrome loaded on the main thread, consumed by the render loop
    private bool pendingUiCollapsed;
    private float pendingUiWidth;
    private volatile bool pendingUiSettings;

    private const float DefaultPanelWidth = 320f;
    private const float MinPanelWidth = 240f;
    private const float MaxPanelWidth = 560f;
    private const float Margin = 10f;
    // fallback when WDE.MapRenderer's view-settings strip hasn't drawn yet (its actual bottom
    // edge is preferred - a hardcoded height overlaps when the strip's size changes)
    private const float FallbackTopOffset = 28f + 2 * 6f + 8f;

    public GameViewInspector(Engine engine,
        ISpawnEditorToolService toolService,
        IGameViewOverlayService overlays,
        IGameNotificationService notifications,
        WDE.Common.Tasks.IMainThread mainThread,
        SpawnInspectorSettings settings)
    {
        this.engine = engine;
        this.toolService = toolService;
        this.overlays = overlays;
        this.notifications = notifications;
        this.mainThread = mainThread;
        this.settings = settings;
        // IUserSettings is main-thread only; the render loop picks the result up when it lands
        mainThread.Dispatch(() =>
        {
            (pendingUiCollapsed, pendingUiWidth) = settings.Load();
            pendingUiSettings = true;
        });
    }

    private void PersistUi() => mainThread.Dispatch(() => settings.Save(collapsed, panelWidth));

    public void RenderGUI()
    {
        if (pendingUiSettings)
        {
            pendingUiSettings = false;
            collapsed = pendingUiCollapsed;
            panelWidth = Math.Clamp(pendingUiWidth, MinPanelWidth, MaxPanelWidth);
        }

        var section = overlays.GetSection(toolService.ActiveTool);

        if (!ImGui.Begin("3D"))
        {
            ImGui.End();
            return;
        }

        var view = engine.GameView.ViewRect;

        // start below the view-settings strip's REAL bottom edge (both dock top-right)
        float stripBottom = WDE.MapRenderer.Modules.ViewSettingsToolbar.LastStripBottom;
        float topOffset = stripBottom > view.Y && stripBottom < view.Y + view.Height * 0.5f
            ? stripBottom - view.Y + 8f
            : FallbackTopOffset;

        DrawHintBar(view, section);

        if (section != null)
        {
            if (collapsed)
                DrawCollapsedTab(view, section, topOffset);
            else
                DrawPanel(view, section, topOffset);
        }

        ImGui.End();
    }

    private void DrawPanel(RectangleF view, IInspectorSection section, float topOffset)
    {
        float width = MathF.Min(panelWidth, view.Width * 0.45f);
        float maxHeight = MathF.Max(120f, view.Height - topOffset - 3 * Margin - ImGui.GetTextLineHeight() * 2);

        ImGui.SetCursorScreenPos(new Vector2(view.Right - width - Margin, view.Y + Margin + topOffset));
        ImGui.SetNextWindowSizeConstraints(new Vector2(width, 0), new Vector2(width, maxHeight));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.WindowBg, 0.85f));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 6f);
        if (ImGui.BeginChild("##inspector", new Vector2(width, 0),
                ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.AlwaysUseWindowPadding))
        {
            // header: section title (+ dirty badge and save/revert) + right-aligned collapse arrow
            ImGui.TextDisabled(section.Title);
            if (section.IsDirty)
            {
                ImGui.SameLine();
                ImGui.TextColored(EditorTheme.Warning, "●");
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("This editor has unsaved changes - Save in the toolbar applies them");
                DrawSaveRevert(section);
            }
            ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.GetFrameHeight() - 8);
            if (ImGui.ArrowButton("##collapse", ImGuiDir.Right))
            {
                collapsed = true;
                PersistUi();
            }
            ImGui.Separator();

            section.DrawContent();

            DrawRevertConfirm(section);
        }
        ImGui.EndChild();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();

        var panelMin = ImGui.GetItemRectMin();
        var panelMax = ImGui.GetItemRectMax();
        BlockClicks(panelMin, panelMax);

        // left-edge resize handle (the panel is right-docked, so only this edge can move)
        ImGui.SetCursorScreenPos(new Vector2(panelMin.X - 4, panelMin.Y));
        ImGui.InvisibleButton("##inspector_resize", new Vector2(8, MathF.Max(8, panelMax.Y - panelMin.Y)));
        bool resizing = ImGui.IsItemActive();
        if (ImGui.IsItemHovered() || resizing)
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
        if (resizing)
        {
            panelWidth = Math.Clamp(panelWidth - ImGui.GetIO().MouseDelta.X, MinPanelWidth, MaxPanelWidth);
            ImGui.GetWindowDrawList().AddLine(new Vector2(panelMin.X, panelMin.Y + 4), new Vector2(panelMin.X, panelMax.Y - 4),
                ImGui.GetColorU32(ImGuiCol.SeparatorActive), 2f);
        }
        if (ImGui.IsItemDeactivated())
            PersistUi(); // one settings write per drag, not per pixel
        BlockClicks(new Vector2(panelMin.X - 4, panelMin.Y), new Vector2(panelMin.X + 4, panelMax.Y));
    }

    // per-editor save/revert next to the dirty badge - saves/discards ONLY this tool's changes,
    // unlike the toolbar Save which flushes every editor at once
    private void DrawSaveRevert(IInspectorSection section)
    {
        bool busy = sectionOpTask is { IsCompleted: false };
        var save = section.SaveSelf;
        var revert = section.RevertSelf;

        if (save != null)
        {
            ImGui.SameLine();
            ImGui.BeginDisabled(busy);
            if (ImGui.SmallButton("Save##section"))
            {
                bool quiet = section.SaveReportsItself;
                string title = section.Title;
                sectionOpTask = save().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        notifications.Notify(GameNotificationType.Error,
                            $"Failed to save {title}: {t.Exception?.GetBaseException().Message}");
                    else if (!quiet)
                        notifications.Notify(GameNotificationType.Success, $"{title} saved");
                });
            }
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(busy ? "Working..." : $"Save only {section.Title} changes");
        }

        if (revert != null)
        {
            ImGui.SameLine();
            ImGui.BeginDisabled(busy);
            if (ImGui.SmallButton("Revert##section"))
                openRevertPopup = true;
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(busy ? "Working..." : "Discard this editor's unsaved changes\nand reload from the database");
        }
    }

    private void DrawRevertConfirm(IInspectorSection section)
    {
        if (openRevertPopup)
        {
            ImGui.OpenPopup("Revert changes");
            openRevertPopup = false;
        }

        bool open = true;
        if (!ImGuiEx.BeginPopupModal("Revert changes", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextUnformatted($"Discard the unsaved {section.Title} changes?");
        ImGui.TextDisabled("The editor reloads from the database. This cannot be undone.");
        ImGui.Separator();
        if (ImGui.Button("Revert", new Vector2(120, 0)))
        {
            var revert = section.RevertSelf;
            string title = section.Title;
            if (revert != null)
                sectionOpTask = revert().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        notifications.Notify(GameNotificationType.Error,
                            $"Failed to revert {title}: {t.Exception?.GetBaseException().Message}");
                    else
                        notifications.Notify(GameNotificationType.Success, $"{title} reverted");
                });
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawCollapsedTab(RectangleF view, IInspectorSection section, float topOffset)
    {
        // a labeled tab hugging the right edge - a bare arrow proved too easy to lose
        string label = section.Title;
        var textSize = ImGui.CalcTextSize(label);
        var pad = new Vector2(10, 5);
        float arrowSpace = 12;
        float dotSpace = section.IsDirty ? 10 : 0;
        var size = new Vector2(textSize.X + arrowSpace + dotSpace + pad.X * 2, textSize.Y + pad.Y * 2);
        var min = new Vector2(view.Right - size.X - Margin, view.Y + Margin + topOffset);

        ImGui.SetCursorScreenPos(min);
        if (ImGui.InvisibleButton("##expand", size))
        {
            collapsed = false;
            PersistUi();
        }
        bool hovered = ImGui.IsItemHovered();
        if (hovered)
            ImGui.SetTooltip(section.IsDirty ? "Show the panel (unsaved changes)" : "Show the panel");

        var dl = ImGui.GetWindowDrawList();
        var max = min + size;
        dl.AddRectFilled(min, max, ImGui.GetColorU32(hovered ? ImGuiCol.ButtonHovered : ImGuiCol.WindowBg, hovered ? 1f : 0.85f), 6f);
        dl.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border, 0.6f), 6f);
        // ◂ expand arrow + the section title
        float cy = (min.Y + max.Y) * 0.5f;
        float ax = min.X + pad.X;
        dl.AddTriangleFilled(new Vector2(ax, cy), new Vector2(ax + 7, cy - 5), new Vector2(ax + 7, cy + 5),
            ImGui.GetColorU32(ImGuiCol.Text, 0.9f));
        dl.AddText(new Vector2(ax + arrowSpace, min.Y + pad.Y), ImGui.GetColorU32(ImGuiCol.Text, 0.95f), label);
        if (section.IsDirty)
            dl.AddCircleFilled(new Vector2(ax + arrowSpace + textSize.X + 6, cy), 3f,
                ImGui.ColorConvertFloat4ToU32(EditorTheme.Warning));

        BlockClicks(min, max);
    }

    private int seenRefusalCounter;
    private string? refusalFlash;
    private double refusalFlashAt;
    private const float RefusalFlashSeconds = 2.5f;

    private static string RefusalText(SpawnEditorTool tool, GizmoMode mode) => (tool, mode) switch
    {
        (SpawnEditorTool.Waypoint, GizmoMode.Rotate) => "Path points have no orientation - nothing to rotate",
        (_, GizmoMode.Scale) => "Spawns have no scale column in the database - nothing to scale",
        _ => "This tool has no rotate gizmo",
    };

    private unsafe void DrawHintBar(RectangleF view, IInspectorSection? section)
    {
        // a refused gizmo-mode keypress (R in a rotation-less tool) briefly takes over the bar -
        // a silently swallowed key reads as a broken key
        if (toolService.ModeRefusalCounter != seenRefusalCounter)
        {
            seenRefusalCounter = toolService.ModeRefusalCounter;
            refusalFlash = RefusalText(toolService.ActiveTool, toolService.LastRefusedMode);
            refusalFlashAt = ImGui.GetTime();
        }
        bool flashing = refusalFlash != null && ImGui.GetTime() - refusalFlashAt < RefusalFlashSeconds;
        if (!flashing)
            refusalFlash = null;

        var hints = flashing ? refusalFlash : section?.Hints;
        if (string.IsNullOrEmpty(hints))
            return;

        var pad = new Vector2(10, 4);
        // wider than the view (long drag hints, tiny window): wrap instead of clipping both ends
        float wrapWidth = MathF.Max(80f, view.Width - 2 * Margin - pad.X * 2);
        var textSize = ImGui.CalcTextSize(hints, false, wrapWidth);
        var size = textSize + pad * 2;
        var min = new Vector2(view.X + (view.Width - size.X) * 0.5f, view.Bottom - size.Y - Margin);
        var max = min + size;
        float rounding = MathF.Min(size.Y * 0.5f, ImGui.GetTextLineHeight()); // stays a pill for one line

        var dl = ImGui.GetWindowDrawList();
        var accent = ImGui.ColorConvertFloat4ToU32(EditorTheme.Warning);
        dl.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.WindowBg, 0.80f), rounding);
        dl.AddRect(min, max, flashing ? accent : ImGui.GetColorU32(ImGuiCol.Border, 0.6f), rounding);
        dl.AddText(ImGui.GetFont(), ImGui.GetFontSize(), min + pad,
            flashing ? accent : ImGui.GetColorU32(ImGuiCol.Text, 0.9f), hints, wrapWidth);

        BlockClicks(min, max);
    }

    private void BlockClicks(Vector2 min, Vector2 max) =>
        engine.GameView.BlockClicksOver(new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y));
}
