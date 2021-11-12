using TheMaths;

namespace TheEngine.Interfaces
{
    public interface IFontManager
    {
        Vector2 MeasureText(string font, ReadOnlySpan<char> text, float fontSize);
    }
}