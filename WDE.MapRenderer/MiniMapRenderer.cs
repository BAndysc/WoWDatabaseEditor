using Avalonia;
using Avalonia.Media;
using WDE.WorldMap;

namespace WDE.MapRenderer
{
    public class MiniMapRenderer : FastBoxRendererControl<GameCameraViewModel, GameViewModel>
    {
        public static readonly DirectProperty<MiniMapRenderer, GameViewModel?> Context2Property = ContextProperty.AddOwner<MiniMapRenderer>(o => o.Context, (o, v) => o.Context = v);
        public GameViewModel? Context2 { get => Context; set => Context = value; }

        protected override Rect DrawItem(DrawingContext ctx, GameCameraViewModel item)
        {
            var editorCoords = CoordsUtils.WorldToEditor(item.X, item.Y);

            double r = 12 / ZoomBias;

            var dotRect = new Rect(editorCoords.editorX - r / 2, editorCoords.editorY - r / 2, r, r);
            
            var color = Context!.SelectedItem == item ? Brushes.Blue : Brushes.Red;
            
            ctx.FillRectangle(color, dotRect, (float)r);
            
            return dotRect;
        }
    }
}