using Avalonia;
using Avalonia.Media;
using WDE.PathPreviewTool.ViewModels;
using WDE.WorldMap;

namespace WDE.PathPreviewTool.Views;

public class MiniMapPathRenderer : FastBoxRendererControl<PathViewModel, PathPreviewViewModel>
{
    public static readonly DirectProperty<MiniMapPathRenderer, PathPreviewViewModel?> Context2Property = ContextProperty.AddOwner<MiniMapPathRenderer>(o => o.Context, (o, v) => o.Context = v);
    public PathPreviewViewModel? Context2 { get => Context; set => Context = value; }

    protected override Rect DrawItem(DrawingContext ctx, PathViewModel item)
    {
        var editorCoords = CoordsUtils.WorldToEditor(item.X, item.Y);

        double r = 12 / ZoomBias;

        var dotRect = new Rect(editorCoords.editorX - r / 2, editorCoords.editorY - r / 2, r, r);
            
        var color = Context!.SelectedItem == item ? Brushes.Blue : Brushes.Red;
            
        ctx.FillRectangle(color, dotRect, (float)r);
            
        return dotRect;
    }
}