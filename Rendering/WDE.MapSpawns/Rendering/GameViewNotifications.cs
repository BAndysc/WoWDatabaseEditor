using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using TheEngine;
using TheMaths;
using WDE.MapSpawns.Models;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// Draws <see cref="IGameNotificationService"/> toasts inside the "3D" window: stacked bottom-center
/// just above the hint bar, newest at the bottom. Successes fade out after a few seconds; errors
/// linger much longer. Hovering a toast pauses its timer, clicking dismisses it. Same in-view
/// technique as <see cref="GameViewToolbar"/> (append into the engine's window +
/// <see cref="TheEngine.Managers.IEngineView.BlockClicksOver"/>). Rendered from
/// <see cref="SpawnViewer.RenderGUI"/> after everything else, so toasts draw on top.
/// </summary>
public class GameViewNotifications
{
    private readonly Engine engine;
    private readonly IGameNotificationService notifications;

    private const float SuccessSeconds = 4f;
    private const float ErrorSeconds = 15f;
    private const float FadeSeconds = 0.4f;
    private const float MaxTextWidth = 420f;
    // the hint bar hugs the bottom edge - stack above it
    private const float BottomOffset = 46f;
    private const float Spacing = 6f;
    private const int MaxToasts = 6;

    private static readonly Vector4 SuccessBg = new(0.09f, 0.30f, 0.14f, 0.92f);
    private static readonly Vector4 SuccessAccent = new(0.35f, 0.85f, 0.45f, 1f);
    private static readonly Vector4 ErrorBg = new(0.35f, 0.10f, 0.10f, 0.94f);
    private static readonly Vector4 ErrorAccent = new(0.95f, 0.35f, 0.35f, 1f);
    private static readonly Vector4 InfoBg = new(0.13f, 0.18f, 0.28f, 0.92f);
    private static readonly Vector4 InfoAccent = new(0.45f, 0.65f, 0.95f, 1f);

    private class Toast
    {
        public GameNotificationType Type;
        public string Message = "";
        public float Remaining;
    }

    private readonly List<Toast> active = new();

    public GameViewNotifications(Engine engine, IGameNotificationService notifications)
    {
        this.engine = engine;
        this.notifications = notifications;
    }

    public void RenderGUI()
    {
        while (notifications.TryDequeue(out var n))
        {
            active.Add(new Toast
            {
                Type = n.Type,
                Message = n.Message,
                Remaining = n.Type == GameNotificationType.Error ? ErrorSeconds : SuccessSeconds,
            });
            if (active.Count > MaxToasts)
                active.RemoveAt(0);
        }

        if (active.Count == 0)
            return;

        if (!ImGui.Begin("3D"u8))
        {
            ImGui.End();
            return;
        }

        float delta = ImGui.GetIO().DeltaTime;
        var view = engine.GameView.ViewRect;
        var dl = ImGui.GetWindowDrawList();
        var pad = new Vector2(12, 8);
        float wrapWidth = MathF.Min(MaxTextWidth, view.Width * 0.7f);
        float y = view.Bottom - BottomOffset;

        // newest toast sits at the bottom, older ones stack upwards
        for (int i = active.Count - 1; i >= 0; --i)
        {
            var toast = active[i];
            bool error = toast.Type == GameNotificationType.Error;
            bool info = toast.Type == GameNotificationType.Info;

            var textSize = ImGui.CalcTextSize(toast.Message, false, wrapWidth);
            var size = textSize + pad * 2 + new Vector2(4, 0); // +4: the accent bar on the left
            var min = new Vector2(view.X + (view.Width - size.X) * 0.5f, y - size.Y);
            var max = min + size;
            y = min.Y - Spacing;

            bool hovered = ImGui.IsMouseHoveringRect(min, max);
            if (!hovered)
                toast.Remaining -= delta; // hover pauses expiry so long errors stay readable
            if (toast.Remaining <= 0 || (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left)))
            {
                active.RemoveAt(i);
                continue;
            }

            float alpha = MathF.Min(1f, toast.Remaining / FadeSeconds);
            var bg = error ? ErrorBg : info ? InfoBg : SuccessBg;
            var accent = error ? ErrorAccent : info ? InfoAccent : SuccessAccent;

            dl.AddRectFilled(min, max, ImGui.GetColorU32(bg with { W = bg.W * alpha }), 6f);
            dl.AddRect(min, max, ImGui.GetColorU32(accent with { W = 0.6f * alpha }), 6f);
            dl.AddRectFilled(min, new Vector2(min.X + 4, max.Y), ImGui.GetColorU32(accent with { W = alpha }), 6f);

            ImGui.SetCursorScreenPos(min + pad + new Vector2(4, 0));
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrapWidth);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 0.95f * alpha));
            ImGui.TextUnformatted(toast.Message);
            ImGui.PopStyleColor();
            ImGui.PopTextWrapPos();

            if (hovered)
                ImGui.SetTooltip("Click to dismiss"u8);

            // clicks on a toast must not fall through into the world underneath
            engine.GameView.BlockClicksOver(new RectangleF(min.X, min.Y, size.X, size.Y));
        }

        ImGui.End();
    }
}
