using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;
using WDE.MapSpawns.Rendering;
using WDE.MapSpawns.ViewModels;
using SpawnEntry = WDE.MapSpawns.ViewModels.SpawnEntry;

namespace WDE.MapSpawns.Views;

public class SpawnsFastTreeList : FastTreeView<SpawnEntry, SpawnInstance>
{
    #region Avalonia workaround
    // apparently control inheritance in Avalonia, when controls are in different projects doesn't work
    #pragma warning disable AVP1013
    #pragma warning disable AVP1022
    public static readonly StyledProperty<FlatTreeList<SpawnEntry, SpawnInstance>?> Items2Property = FastTreeView<SpawnEntry, SpawnInstance>.ItemsProperty.AddOwner<SpawnsFastTreeList>();
    public static readonly StyledProperty<INodeType?> SelectedSpawn2Property = FastTreeView<SpawnEntry, SpawnInstance>.SelectedNodeProperty.AddOwner<SpawnsFastTreeList>();
    #pragma warning restore AVP1013
    #pragma warning restore AVP1022

    public FlatTreeList<SpawnEntry, SpawnInstance>? Items2
    {
        get => GetValue(Items2Property);
        set => SetValue(Items2Property, value);
    }

    public INodeType? SelectedSpawn2
    {
        get => GetValue(SelectedSpawn2Property);
        set => SetValue(SelectedSpawn2Property, value);
    }
    #endregion
    
    static SpawnsFastTreeList()
    {
        SelectedSpawn2Property.Changed.AddClassHandler<SpawnsFastTreeList>((tree, args) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (args.NewValue is SpawnInstance newSelection)
                {
                    var index = tree.Items!.IndexOf(newSelection);
                    if (index == -1)
                        index = ((IList<INodeType>)tree.Items!).IndexIf(x => x is SpawnEntry group && group.Entry == newSelection.Entry);
                    if (index != -1)
                    {
                        float y = index * RowHeight;
                        if (tree.ScrollViewer != null)
                        {
                            if (tree.ScrollViewer.Offset.Y > y || tree.ScrollViewer.Offset.Y + tree.ScrollViewer.Viewport.Height < y)
                                tree.ScrollViewer.Offset = tree.ScrollViewer.Offset.WithY(y);   
                        }
                    }
                
                    tree.InvalidateVisual();   
                }
            }, DispatcherPriority.Render);
        });
    }
    
    protected override void DrawRow(Typeface typeface, Pen pen, IBrush foreground, DrawingContext context, object? row, Rect rect)
    {
        {
            var ft = new FormattedText(row?.ToString() ?? "(null)",
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12, foreground)
            {
                MaxTextWidth = float.MaxValue,
                MaxTextHeight = RowHeight
            };

            var isHover = mouseOverRow == row;
            var isSelected = SelectedSpawn2 == row;
        
            if (isHover || isSelected)
                context.DrawRectangle(isHover ? HoverRowBackground : SelectedRowBackground, null, rect);

            if (row is SpawnEntry group)
            {
                context.DrawText(ft, new Point(Indent, rect.Y + rect.Height / 2 - ft.Height / 2));
                DrawToggleMark(rect.WithWidth(RowHeight), context, pen, group.IsExpanded);
            }
            else if (row is SpawnInstance spawn)
            {
                context.DrawText(ft, new Point(Indent * 2, rect.Y + rect.Height / 2 - ft.Height / 2));
                double x = rect.Width - 2;
                if (spawn.IsSpawned)
                    context.DrawRectangle(foreground, null, new Rect(x - 6, rect.Center.Y - 3, 6, 6), 3, 3);
            }
        }
    }
}