using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using WDE.PathPreviewTool.ViewModels;
using WDE.WorldMap;

namespace WDE.PathPreviewTool.Views;

public class MiniMapPathRenderer : FastBoxRendererControl<PathViewModel, PathPreviewViewModel>
{
    public static readonly DirectProperty<MiniMapPathRenderer, PathPreviewViewModel?> Context2Property = ContextProperty.AddOwner<MiniMapPathRenderer>(o => o.Context, (o, v) => o.Context = v);
    public PathPreviewViewModel? Context2 { get => Context; set => Context = value; }

    private IPen pen = new ImmutablePen(Brushes.White);
    
    protected override Rect DrawItem(DrawingContext ctx, PathViewModel item)
    {
        var editorCoords = CoordsUtils.WorldToEditor(item.X, item.Y);

        double r = 11 / ZoomBias;
        
        var prev = editorCoords;
        foreach (var p in item.Waypoints)
        {
            var newPoint = CoordsUtils.WorldToEditor(p.X, p.Y);
            ctx.DrawLine(pen, new Point(prev.editorX, prev.editorY), new Point(newPoint.editorX, newPoint.editorY));
            prev = newPoint;
        }

        var dotRect = new Rect(editorCoords.editorX - r / 2, editorCoords.editorY - r / 2, r, r);
            
        var color = Context!.SelectedItem == item ? Brushes.Blue : Brushes.Red;
            
        ctx.FillRectangle(color, dotRect, (float)r);
            
        return dotRect;
    }
}