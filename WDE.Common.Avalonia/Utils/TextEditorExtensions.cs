using AvaloniaEdit.Document;

namespace WDE.Common.Avalonia.Utils;

public static class TextEditorExtensions
{
    public static string GetLastWord(this ITextSource src, int endPosition, out int startPos)
    {
        startPos = endPosition;
        while (startPos >= 0 && (src.GetCharAt(startPos) is var chr && (char.IsLetterOrDigit(chr) || chr == '_' || chr == '$')))
            startPos--;
        startPos++;
        if (endPosition - startPos + 1 <= 0)
            return "";
        return src.GetText(startPos, endPosition - startPos + 1);
    }
}