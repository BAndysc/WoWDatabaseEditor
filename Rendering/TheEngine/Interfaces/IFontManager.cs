using TheMaths;

namespace TheEngine.Interfaces
{
    public interface IFontManager
    {
        void DrawText(string font, ReadOnlySpan<char> text, float fontSize, float x, float y, float? maxWidth);
        Vector2 MeasureText(string font, ReadOnlySpan<char> text, float fontSize);
        void DrawBox(float x, float y, float w, float h, Vector4 color);
    }
}