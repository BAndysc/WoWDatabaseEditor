using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Prism.Regions;
using WDE.Common.Avalonia.Components;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;
using WoWDatabaseEditorCore.Services.OutlinerTool;

namespace WoWDatabaseEditorCore.Avalonia.Services.OutlinerTool;

public class OutlinerFastTreeView : FastTreeView<OutlinerGroupViewModel, OutlinerItemViewModel>
{
    //Avalonia BUG workaround
    public static readonly StyledProperty<FlatTreeList<OutlinerGroupViewModel, OutlinerItemViewModel>?> Items2Property = FastTreeView<OutlinerGroupViewModel, OutlinerItemViewModel>.ItemsProperty.AddOwner<OutlinerFastTreeView>();
    public static readonly StyledProperty<INodeType?> SelectedNode2Property = FastTreeView<OutlinerGroupViewModel, OutlinerItemViewModel>.SelectedNodeProperty.AddOwner<OutlinerFastTreeView>();
    
    public FlatTreeList<OutlinerGroupViewModel, OutlinerItemViewModel>? Items2
    {
        get => GetValue(Items2Property);
        set => SetValue(Items2Property, value);
    }

    public INodeType? SelectedNode2
    {
        get => GetValue(SelectedNode2Property);
        set => SetValue(SelectedNode2Property, value);
    }
    
    protected override void DrawRow(Typeface typeface, Pen pen, IBrush foreground, DrawingContext context, object? row, Rect rect)
    {
        var isHover = mouseOverRow == row;
        var isSelected = SelectedNode2 == row;
        
        if (isHover || isSelected)
            context.DrawRectangle(isHover ? HoverRowBackground : SelectedRowBackground, null, rect);

        var width = Bounds.Width;
        
        if (row is OutlinerGroupViewModel group)
        {
            var toggleRect = rect.WithX(Indent * (group.NestLevel - 1)).WithWidth(RowHeight);

            
            var ft = new FormattedText(group.Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12, foreground)
            {
                MaxTextWidth = float.PositiveInfinity,
                MaxTextHeight = RowHeight
            };
            var clip = context.PushClip(new Rect(toggleRect.Right, rect.Y, width - toggleRect.Right, rect.Height));
            context.DrawText(ft, new Point(Indent * group.NestLevel, rect.Y + rect.Height / 2 - ft.Height / 2));
            clip.Dispose();

            if (group.Items.Count > 0)
                DrawToggleMark(toggleRect, context, pen, group.IsExpanded);
        }
        else if (row is OutlinerItemViewModel item)
        {
            var image = WdeImage.LoadBitmap(item.Icon);
            float x = Indent * (item.NestLevel - 1);
            var iconRect = rect.WithX(x).WithY(rect.Y + RowHeight / 2 - 16 / 2).WithWidth(16).WithHeight(16);
            if (image != null)
                context.DrawImage(image, new Rect(0, 0, image!.Size.Width, image.Size.Height), iconRect);
            x += Indent;
            if (item.Entry != null)
            {
                
                var ft = new FormattedText(item.Entry, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(typeface.FontFamily, FontStyle.Normal, FontWeight.Bold), 12, foreground)
                {
                    MaxTextWidth = float.PositiveInfinity,
                    MaxTextHeight = RowHeight
                };

                context.DrawText(ft, new Point(x, rect.Y + rect.Height / 2 - ft.Height / 2));
                x += (float)ft.Width + 10;

            }

            var ft2 = new FormattedText(item.Name ?? "(null)", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12, foreground)
            {
                MaxTextWidth = float.PositiveInfinity,
                MaxTextHeight = RowHeight
            };
            context.DrawText(ft2, new Point(x, rect.Y + rect.Height / 2 - ft2.Height / 2));
        }
    }
}