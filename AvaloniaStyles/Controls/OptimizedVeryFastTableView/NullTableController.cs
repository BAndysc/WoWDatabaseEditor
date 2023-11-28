using Avalonia;
using Avalonia.Media;

namespace AvaloniaStyles.Controls.OptimizedVeryFastTableView;

internal class NullTableController : BaseVirtualizedTableController
{
    public override bool PointerDown(int rowIndex, int cellIndex, Rect cellRect, Point pressPoint, bool leftPressed, bool rightPressed, int clickCount)
    {
        return false;
    }

    public override bool PointerUp(int rowIndex, int cellIndex, Rect cellRect, Point pressPoint, bool leftPressed, bool rightPressed)
    {
        return false;
    }

    public override bool SpawnEditorFor(int rowIndex, int cellIndex, Rect cellRect, string? typedText, VirtualizedVeryFastTableView view)
    {
        return false;
    }

    public override string? GetCellText(int rowIndex, int cellIndex)
    {
        return null;
    }

    public override void DrawRow(int rowIndex, Rect rowRect, DrawingContext drawingContext, VirtualizedVeryFastTableView view)
    {
    }

    public override bool Draw(int rowIndex, int cellIndex, DrawingContext drawingContext, VirtualizedVeryFastTableView view, ref Rect rect)
    {
        return false;
    }

    public override void DrawHeader(int cellIndex, DrawingContext context, VirtualizedVeryFastTableView view, ref Rect rect)
    {
    }
}