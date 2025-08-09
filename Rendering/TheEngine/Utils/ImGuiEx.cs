using ImGuiNET;

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
        byte* p_open = null;
        fixed (byte* namePtr = name)
        {
            int num = ImGuiNative.igBegin(namePtr, p_open, flags);
            return (uint) num > 0U;
        }
    }

    public static unsafe bool Begin(ReadOnlySpan<byte> name, ref bool isOpen, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        VerifyCString(name);
        byte p_open = isOpen ? (byte)1 : (byte)0;
        fixed (byte* namePtr = name)
        {
            int num = ImGuiNative.igBegin(namePtr, &p_open, flags);
            isOpen = p_open > 0;
            return (uint) num > 0U;
        }
    }

    public static unsafe bool BeginCombo(ReadOnlySpan<byte> label, ReadOnlySpan<byte> preview_value, ImGuiComboFlags flags = 0)
    {
        VerifyCString(label);
        VerifyCString(preview_value);
        fixed (byte* numPtr1 = label)
        {
            fixed (byte* numPtr2 = preview_value)
            {
                int num = (int) ImGuiNative.igBeginCombo(numPtr1, numPtr2, flags);
                return (uint) num > 0U;
            }
        }
    }

    public static unsafe bool Selectable(ReadOnlySpan<byte> label, bool selected)
    {
        VerifyCString(label);
        byte selected1 = selected ? (byte)1 : (byte)0;
        ImGuiSelectableFlags flags = ImGuiSelectableFlags.None;
        Vector2 size = new Vector2();
        fixed (byte* numPtr = label)
        {
            int num = (int) ImGuiNative.igSelectable_Bool(numPtr, selected1, flags, size);
            return (uint) num > 0U;
        }
    }

    public static unsafe bool Button(ReadOnlySpan<byte> label)
    {
        VerifyCString(label);
        Vector2 size = new Vector2();
        fixed (byte* numPtr = label)
        {
            int num = (int) ImGuiNative.igButton(numPtr, size);
            return (uint) num > 0U;
        }
    }

    public static unsafe void TextUnformatted(ReadOnlySpan<byte> text)
    {
        VerifyCString(text);
        if (text == default)
        {
            ImGuiNative.igTextUnformatted(null, null);
            return;
        }

        fixed (byte* numPtr = text)
        {
            ImGuiNative.igTextUnformatted(numPtr, numPtr + text.Length - 1);
        }
    }

    public static unsafe bool MenuItem(ReadOnlySpan<byte> label, ReadOnlySpan<byte> shortcut, ref bool p_selected)
    {
        VerifyCString(label);
        VerifyCString(shortcut);
        byte num1 = p_selected ? (byte)1 :(byte)0;
        byte* p_selected1 = &num1;
        byte enabled = 1;
        fixed (byte* numPtr1 = label)
        {
            fixed (byte* numPtr2 = shortcut)
            {
                int num2 = (int) ImGuiNative.igMenuItem_BoolPtr(numPtr1, numPtr2, p_selected1, enabled);
                p_selected = num1 > (byte) 0;
                return (uint) num2 > 0U;
            }
        }
    }

    public static unsafe bool BeginMenu(ReadOnlySpan<byte> label)
    {
        VerifyCString(label);
        byte enabled = 1;
        fixed (byte* numPtr = label)
        {
            int num = (int) ImGuiNative.igBeginMenu(numPtr, enabled);
            return (uint) num > 0U;
        }
    }
}