using Hexa.NET.ImGui;

namespace TheEngine;

public static class ImGuiEx
{
    private static void VerifyCString(ReadOnlySpan<byte> str)
    {
        if (str != default && str[^1] != 0)
        {
            throw new Exception("Null terminated string is expected!");
        }
    }

    public static unsafe bool Begin(ReadOnlySpan<byte> name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        VerifyCString(name);
        return ImGui.Begin(name, flags);
    }

    public static unsafe bool Begin(ReadOnlySpan<byte> name, ref bool isOpen, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        VerifyCString(name);
        return ImGui.Begin(name, ref isOpen, flags);
    }

    public static unsafe bool BeginCombo(ReadOnlySpan<byte> label, ReadOnlySpan<byte> preview_value, ImGuiComboFlags flags = 0)
    {
        VerifyCString(label);
        VerifyCString(preview_value);
        return ImGui.BeginCombo(label, preview_value, flags);
    }

    public static unsafe bool Selectable(ReadOnlySpan<byte> label, bool selected)
    {
        VerifyCString(label);
        return ImGui.Selectable(label, ref selected);
    }

    public static unsafe bool Selectable(ReadOnlySpan<byte> label, bool selected, System.Numerics.Vector2 size)
    {
        VerifyCString(label);
        return ImGui.Selectable(label, ref selected, ImGuiSelectableFlags.None, size);
    }

    public static unsafe bool Button(ReadOnlySpan<byte> label)
    {
        VerifyCString(label);
        return ImGui.Button(label);
    }

    public static unsafe void TextUnformatted(ReadOnlySpan<byte> text)
    {
        ImGui.TextUnformatted(text);
    }

    public static unsafe bool MenuItem(ReadOnlySpan<byte> label, ReadOnlySpan<byte> shortcut, ref bool p_selected)
    {
        VerifyCString(label);
        VerifyCString(shortcut);
        return ImGui.MenuItem(label, shortcut, ref p_selected);
    }

    public static unsafe bool BeginMenu(ReadOnlySpan<byte> label)
    {
        VerifyCString(label);
        return ImGui.BeginMenu(label);
    }

    // ---- Escape handling --------------------------------------------------------------------
    // ImGui keyboard nav is off (it would fight the 3D view for the keyboard), so Dear ImGui never
    // closes popups on Escape by itself. These Begin wrappers do it instead, and world-level escape
    // consumers (the spawn editor's step-out chain, tool-local escapes) stand down while
    // AnyPopupOpen, so one press never closes a popup AND acts on the world underneath.

    /// <summary>True while any ImGui popup, modal or context menu is open anywhere.</summary>
    public static bool AnyPopupOpen => ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup);

    /// <summary>Escape pressed, and no active text field claims it (Escape inside a field reverts
    /// the edit; only the next press escapes further out).</summary>
    private static bool EscapePressed() => ImGui.IsKeyPressed(ImGuiKey.Escape) && !ImGui.GetIO().WantTextInput;

    private static void CloseCurrentPopupOnEscape()
    {
        // focus-gated: a nested popup open on top (a combo, a menu) owns the Escape instead
        if (EscapePressed() && ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
            ImGui.CloseCurrentPopup();
    }

    /// <summary>ImGui.BeginPopup that also closes the popup on Escape (while it is the focused one).</summary>
    public static bool BeginPopup(string strId, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        if (!ImGui.BeginPopup(strId, flags))
            return false;
        CloseCurrentPopupOnEscape();
        return true;
    }

    /// <summary>ImGui.BeginPopupModal that also closes the modal on Escape (like its Cancel/close button).</summary>
    public static bool BeginPopupModal(string name, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        if (!ImGui.BeginPopupModal(name, ref open, flags))
            return false;
        CloseCurrentPopupOnEscape();
        return true;
    }

    /// <summary>The same step-out for a plain closable window (the spawn picker, the quick tour) -
    /// call inside its Begin body: true when Escape should close it, i.e. the window is focused and
    /// no popup or text edit owns the key.</summary>
    public static bool WindowWantsCloseOnEscape()
    {
        return EscapePressed()
               && ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows)
               && !AnyPopupOpen;
    }
}