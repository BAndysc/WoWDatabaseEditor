using System;
using System.Numerics;
using Hexa.NET.ImGui;

namespace WDE.MapSpawns.Rendering;

public enum ToolIcon
{
    Select,
    Waypoint,
    Formation,
    SpawnGroup,
    Pool,
    Graveyard,
    SpellTarget,
    CreatureLink,
    Save,
    Undo,
    Redo,
    Translate,
    Rotate,
    Scale,
    Search,
    Keyboard,
}

/// <summary>
/// Vector tool icons drawn with ImDrawList primitives (no texture assets, crisp at any scale,
/// tinted by the current style). Used by <see cref="GameViewToolbar"/>'s in-view icon buttons.
/// Button mechanics live in the shared <see cref="WDE.MapRenderer.Utils.ImGuiIconButtons"/> -
/// only the glyphs are this file's.
/// </summary>
public static class ToolIcons
{
    /// <summary>An icon button: invisible button + custom-drawn background and glyph. Returns true
    /// when clicked (never while disabled). Disabled buttons still show their tooltip.</summary>
    public static bool IconButton(string id, ToolIcon icon, float size, bool active, string? tooltip, bool enabled = true, uint activeColor = 0)
    {
        var slot = WDE.MapRenderer.Utils.ImGuiIconButtons.IconButton(id, size, active, tooltip, enabled, activeColor);
        DrawIcon(ImGui.GetWindowDrawList(), icon, slot.GlyphOrigin, slot.GlyphSize, slot.GlyphColor);
        return slot.Clicked;
    }

    /// <summary>Draws a bare glyph (no button) - used for menu rows, badges, ...</summary>
    public static void DrawIcon(ImDrawListPtr dl, ToolIcon icon, Vector2 origin, float s, uint col)
    {
        float th = WDE.MapRenderer.Utils.ImGuiIconButtons.Stroke(s);
        Vector2 P(float x, float y) => origin + new Vector2(x, y) * s;

        switch (icon)
        {
            case ToolIcon.Select:
            {
                // classic pointer: solid wedge + short tail
                dl.AddTriangleFilled(P(0.28f, 0.05f), P(0.28f, 0.78f), P(0.66f, 0.52f), col);
                var dir = Vector2.Normalize(new Vector2(0.42f, 0.90f));
                var n = new Vector2(-dir.Y, dir.X) * s * 0.055f;
                var a = P(0.46f, 0.55f);
                var b = P(0.64f, 0.92f);
                dl.AddQuadFilled(a - n, a + n, b + n, b - n, col);
                break;
            }
            case ToolIcon.Waypoint:
            {
                // pen-tool path: two nodes, a bend, arrowhead at the end
                var a = P(0.08f, 0.85f);
                var b = P(0.42f, 0.25f);
                var c = P(0.92f, 0.62f);
                dl.AddLine(a, b, col, th);
                dl.AddLine(b, c, col, th);
                dl.AddCircleFilled(a, s * 0.09f, col);
                dl.AddCircleFilled(b, s * 0.09f, col);
                var dir = Vector2.Normalize(c - b);
                var n = new Vector2(-dir.Y, dir.X);
                dl.AddTriangleFilled(c + dir * s * 0.10f, c - dir * s * 0.10f + n * s * 0.11f, c - dir * s * 0.10f - n * s * 0.11f, col);
                break;
            }
            case ToolIcon.Formation:
            {
                // leader in front, two members behind, linked to the leader
                var leader = P(0.50f, 0.20f);
                var m1 = P(0.16f, 0.80f);
                var m2 = P(0.84f, 0.80f);
                dl.AddLine(leader, m1, col, th * 0.8f);
                dl.AddLine(leader, m2, col, th * 0.8f);
                dl.AddCircleFilled(leader, s * 0.15f, col);
                dl.AddCircle(m1, s * 0.11f, col, 0, th);
                dl.AddCircle(m2, s * 0.11f, col, 0, th);
                break;
            }
            case ToolIcon.SpawnGroup:
            {
                // dots inside a dashed marquee (a "grouped selection")
                var min = P(0.02f, 0.02f);
                var max = P(0.98f, 0.98f);
                DashedRect(dl, min, max, col, th * 0.9f);
                dl.AddCircleFilled(P(0.34f, 0.38f), s * 0.10f, col);
                dl.AddCircleFilled(P(0.68f, 0.44f), s * 0.10f, col);
                dl.AddCircleFilled(P(0.46f, 0.70f), s * 0.10f, col);
                break;
            }
            case ToolIcon.Pool:
            {
                // lottery drum: three overlapping "balls", only one comes out (the filled one)
                dl.AddCircle(P(0.32f, 0.34f), s * 0.22f, col, 0, th);
                dl.AddCircle(P(0.70f, 0.42f), s * 0.22f, col, 0, th);
                dl.AddCircleFilled(P(0.46f, 0.72f), s * 0.22f, col);
                break;
            }
            case ToolIcon.Graveyard:
            {
                // tombstone: rounded-top slab with a cross engraved, standing on a ground line
                var c = P(0.5f, 0.34f);
                float r = s * 0.30f;
                dl.PathArcTo(c, r, -MathF.PI, 0, 20);
                dl.PathLineTo(P(0.80f, 0.86f));
                dl.PathLineTo(P(0.20f, 0.86f));
                dl.PathLineTo(P(0.20f, 0.34f));
                dl.PathStroke(col, ImDrawFlags.Closed, th);
                dl.AddLine(P(0.5f, 0.26f), P(0.5f, 0.58f), col, th * 0.9f);
                dl.AddLine(P(0.36f, 0.38f), P(0.64f, 0.38f), col, th * 0.9f);
                dl.AddLine(P(0.06f, 0.94f), P(0.94f, 0.94f), col, th);
                break;
            }
            case ToolIcon.SpellTarget:
            {
                // teleport destination: crosshair ring with ticks and a center dot
                var c = P(0.5f, 0.5f);
                float r = s * 0.34f;
                dl.AddCircle(c, r, col, 0, th);
                dl.AddCircleFilled(c, s * 0.08f, col);
                dl.AddLine(P(0.5f, 0.02f), P(0.5f, 0.22f), col, th);
                dl.AddLine(P(0.5f, 0.78f), P(0.5f, 0.98f), col, th);
                dl.AddLine(P(0.02f, 0.5f), P(0.22f, 0.5f), col, th);
                dl.AddLine(P(0.78f, 0.5f), P(0.98f, 0.5f), col, th);
                break;
            }
            case ToolIcon.CreatureLink:
            {
                // two chain links joined, with an arrow along them (master ← slave reaction)
                dl.AddCircle(P(0.30f, 0.36f), s * 0.20f, col, 0, th);
                dl.AddCircle(P(0.62f, 0.60f), s * 0.20f, col, 0, th);
                var a = P(0.16f, 0.86f);
                var b = P(0.84f, 0.16f);
                dl.AddLine(a, b, col, th);
                ArrowHead(dl, b, Vector2.Normalize(b - a), s, col);
                break;
            }
            case ToolIcon.Save:
            {
                // floppy: outline with a cut corner, shutter on top, label below
                dl.AddLine(P(0.05f, 0.05f), P(0.72f, 0.05f), col, th);
                dl.AddLine(P(0.72f, 0.05f), P(0.95f, 0.28f), col, th);
                dl.AddLine(P(0.95f, 0.28f), P(0.95f, 0.95f), col, th);
                dl.AddLine(P(0.95f, 0.95f), P(0.05f, 0.95f), col, th);
                dl.AddLine(P(0.05f, 0.95f), P(0.05f, 0.05f), col, th);
                dl.AddRect(P(0.26f, 0.05f), P(0.66f, 0.32f), col, 0, 0, th * 0.9f);
                dl.AddRect(P(0.22f, 0.55f), P(0.78f, 0.95f), col, 0, 0, th * 0.9f);
                break;
            }
            case ToolIcon.Translate:
            {
                // 4-way move cross
                dl.AddLine(P(0.5f, 0.10f), P(0.5f, 0.90f), col, th);
                dl.AddLine(P(0.10f, 0.5f), P(0.90f, 0.5f), col, th);
                ArrowHead(dl, P(0.5f, 0.06f), new Vector2(0, -1), s, col);
                ArrowHead(dl, P(0.5f, 0.94f), new Vector2(0, 1), s, col);
                ArrowHead(dl, P(0.06f, 0.5f), new Vector2(-1, 0), s, col);
                ArrowHead(dl, P(0.94f, 0.5f), new Vector2(1, 0), s, col);
                break;
            }
            case ToolIcon.Rotate:
            {
                // ~250 deg circular arrow with a tangential arrowhead
                var c = P(0.5f, 0.5f);
                float r = s * 0.38f;
                const float aMin = -4.0f, aMax = 0.4f;
                dl.PathArcTo(c, r, aMin, aMax, 28);
                dl.PathStroke(col, ImDrawFlags.None, th);
                var end = c + new Vector2(MathF.Cos(aMax), MathF.Sin(aMax)) * r;
                var tangent = new Vector2(-MathF.Sin(aMax), MathF.Cos(aMax)); // direction of increasing angle
                ArrowHead(dl, end + tangent * s * 0.06f, tangent, s, col);
                break;
            }
            case ToolIcon.Scale:
            {
                // small square growing along a diagonal arrow into a corner bracket
                dl.AddRect(P(0.08f, 0.56f), P(0.44f, 0.92f), col, 0, 0, th * 0.9f);
                dl.AddLine(P(0.36f, 0.64f), P(0.82f, 0.18f), col, th);
                ArrowHead(dl, P(0.88f, 0.12f), Vector2.Normalize(new Vector2(1, -1)), s, col);
                dl.AddLine(P(0.60f, 0.08f), P(0.92f, 0.08f), col, th * 0.9f);
                dl.AddLine(P(0.92f, 0.08f), P(0.92f, 0.40f), col, th * 0.9f);
                break;
            }
            case ToolIcon.Search:
            {
                // magnifier: ring + handle toward the bottom-right corner
                var c = P(0.42f, 0.42f);
                float r = s * 0.30f;
                dl.AddCircle(c, r, col, 0, th);
                var dir = Vector2.Normalize(new Vector2(1, 1));
                dl.AddLine(c + dir * r, P(0.92f, 0.92f), col, th * 1.2f);
                break;
            }
            case ToolIcon.Keyboard:
            {
                // keyboard: rounded body, two key rows and a spacebar
                dl.AddRect(P(0.04f, 0.22f), P(0.96f, 0.78f), col, s * 0.08f, 0, th);
                for (int i = 0; i < 4; ++i)
                    dl.AddRectFilled(P(0.14f + i * 0.20f, 0.34f), P(0.24f + i * 0.20f, 0.42f), col);
                for (int i = 0; i < 4; ++i)
                    dl.AddRectFilled(P(0.14f + i * 0.20f, 0.48f), P(0.24f + i * 0.20f, 0.56f), col);
                dl.AddRectFilled(P(0.28f, 0.62f), P(0.72f, 0.69f), col);
                break;
            }
            case ToolIcon.Undo:
            case ToolIcon.Redo:
            {
                // half-circle arrow over the top; Redo is the mirror image
                bool undo = icon == ToolIcon.Undo;
                var c = P(0.5f, 0.58f);
                float r = s * 0.40f;
                dl.PathArcTo(c, r, -MathF.PI, 0, 24);
                dl.PathStroke(col, ImDrawFlags.None, th);
                float endX = undo ? c.X - r : c.X + r;
                var tipBase = new Vector2(endX, c.Y + s * 0.02f);
                dl.AddTriangleFilled(
                    tipBase + new Vector2(0, s * 0.22f),
                    tipBase + new Vector2(-s * 0.14f, -s * 0.04f),
                    tipBase + new Vector2(s * 0.14f, -s * 0.04f),
                    col);
                break;
            }
        }
    }

    // small filled triangle at tip, pointing along dir (normalized); sized relative to the glyph box
    private static void ArrowHead(ImDrawListPtr dl, Vector2 tip, Vector2 dir, float s, uint col)
    {
        var n = new Vector2(-dir.Y, dir.X);
        dl.AddTriangleFilled(tip, tip - dir * s * 0.20f + n * s * 0.11f, tip - dir * s * 0.20f - n * s * 0.11f, col);
    }

    private static void DashedRect(ImDrawListPtr dl, Vector2 min, Vector2 max, uint col, float th)
    {
        const int dashesPerSide = 3;
        float w = max.X - min.X, h = max.Y - min.Y;
        for (int i = 0; i < dashesPerSide; ++i)
        {
            // dashes cover [t0..t1] of each side, leaving gaps between them
            float t0 = (i + 0.15f) / dashesPerSide, t1 = (i + 0.70f) / dashesPerSide;
            dl.AddLine(new Vector2(min.X + w * t0, min.Y), new Vector2(min.X + w * t1, min.Y), col, th);
            dl.AddLine(new Vector2(min.X + w * t0, max.Y), new Vector2(min.X + w * t1, max.Y), col, th);
            dl.AddLine(new Vector2(min.X, min.Y + h * t0), new Vector2(min.X, min.Y + h * t1), col, th);
            dl.AddLine(new Vector2(max.X, min.Y + h * t0), new Vector2(max.X, min.Y + h * t1), col, th);
        }
    }
}
