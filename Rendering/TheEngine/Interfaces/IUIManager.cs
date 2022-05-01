using System;
using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Interfaces
{
        
    public interface IDrawingCommand
    {
        Vector2 Measure();
        Vector2 Draw(float x, float y, float width, float height);
    }
    
    public interface IUIManager
    {
        Entity DrawPersistentWorldText(string font, Vector2 pivot, string text, float fontSize, Matrix localToWorld, float visibilityDistance = 200);
        void DrawWorldText(string font, Vector2 pivot, ReadOnlySpan<char> text, float fontSize, Matrix localToWorld);
        void DrawText(string font, ReadOnlySpan<char> text, float fontSize, float x, float y, float? maxWidth, Vector4 color);
        void DrawBox(float x, float y, float w, float h, Vector4 color);
        Vector2 MeasureText(string font, ReadOnlySpan<char> text, float fontSize);

        IImGui BeginImmediateDrawAbs(float x, float y);
        IImGui BeginImmediateDrawRel(float x, float y, float pivotX, float pivotY);
    }

    public interface IImGui : System.IDisposable
    {
        void BeginVerticalBox(Vector4 color, float padding);
        void BeginHorizontalBox();
        void Text(string font, string str, float size, Vector4 color);
        void EndBox();
        void Rectangle(Vector4 color, float padding, float width, float height);
        void StretchFill(float minWidth, float minHeight);
    }
}