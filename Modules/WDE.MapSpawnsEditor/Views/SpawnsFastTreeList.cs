using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using WDE.Common.Avalonia.Components;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MapSpawnsEditor.ViewModels;

namespace WDE.MapSpawnsEditor.Views;

public class SpawnsFastTreeList : FastTreeView<SpawnGroup, SpawnInstance>
{
    #region Avalonia workaround
    // apparently control inheritance in Avalonia, when controls are in different projects doesn't work
    public static readonly StyledProperty<FlatTreeList<SpawnGroup, SpawnInstance>?> Items2Property = ItemsProperty.AddOwner<SpawnsFastTreeList>();
    public static readonly StyledProperty<INodeType?> SelectedNode2Property = SelectedNodeProperty.AddOwner<SpawnsFastTreeList>();
    public static readonly StyledProperty<bool> IsFiltered2Property = IsFilteredProperty.AddOwner<SpawnsFastTreeList>();

    public FlatTreeList<SpawnGroup, SpawnInstance>? Items2
    {
        get => GetValue(Items2Property);
        set => SetValue(Items2Property, value);
    }

    public INodeType? SelectedNode2
    {
        get => GetValue(SelectedNode2Property);
        set => SetValue(SelectedNode2Property, value);
    }

    public bool IsFiltered2
    {
        get => GetValue(IsFiltered2Property);
        set => SetValue(IsFiltered2Property, value);
    }
    #endregion

    private static Bitmap? zoneAreaIcon;
    private static Bitmap? zoneAreaIconOpened;
    private static Bitmap? mapIcon;
    private static Bitmap? creaturesIcon;
    private static Bitmap? gobjectsIcon;
    private static Bitmap? creatureIcon;
    private static Bitmap? gobjectIcon;
    private static Bitmap? spawnGroupIcon;

    private int GetIndexOf(INodeType node)
    {
        if (!IsFiltered)
            return Items!.IndexOf(node);
        else
        {
            var items = Items;
            var index = 0;
            for (int i = 0; i < items!.Count; ++i)
            {
                if (!items[i].IsVisible)
                    continue;

                if (items[i] == node)
                    return index;
                
                index++;
            }

            return -1;
        }
    }
    
    static SpawnsFastTreeList()
    {
        async Task LoadIconsAsync()
        {
            zoneAreaIcon = await WdeImage.LoadBitmapAsync(new ImageUri("Icons/icon_folder_2.png"));
            zoneAreaIconOpened = await WdeImage.LoadBitmapAsync(new ImageUri("Icons/icon_folder_2_opened.png"));
            mapIcon = await WdeImage.LoadBitmapAsync(new ImageUri("Icons/icon_world.png"));
            creatureIcon = await WdeImage.LoadBitmapAsync(new ImageUri("Icons/document_creature_template.png"));
            gobjectIcon = await WdeImage.LoadBitmapAsync(new ImageUri("Icons/document_gameobject_template.png"));
            creaturesIcon = await WdeImage.LoadBitmapAsync(new ImageUri("Icons/document_creatures.png"));
            gobjectsIcon = await WdeImage.LoadBitmapAsync(new ImageUri("Icons/document_gameobjects.png"));
            spawnGroupIcon = await WdeImage.LoadBitmapAsync(new ImageUri("Icons/icon_spawngroup.png"));
        }
        LoadIconsAsync().ListenErrors();
        SelectedNode2Property.Changed.AddClassHandler<SpawnsFastTreeList>((tree, args) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var newSelection = (INodeType?)args.NewValue;
                if (newSelection == null)
                    return;
                
                var index = tree.GetIndexOf(newSelection);
                var parent = newSelection.Parent;
                while (index == -1 && parent != null)
                {
                    parent.IsExpanded = true;
                    index = tree.GetIndexOf(parent);
                    parent = parent.Parent;
                }
                
                tree.InvalidateMeasure();
                tree.InvalidateVisual();
                
                if (index != -1)
                {
                    DispatcherTimer.RunOnce(() =>
                    {
                        float y = index * RowHeight;
                        if (tree.ScrollViewer != null)
                        {
                            if (tree.ScrollViewer.Offset.Y > y ||
                                tree.ScrollViewer.Offset.Y + tree.ScrollViewer.Viewport.Height < y)
                                tree.ScrollViewer.Offset = tree.ScrollViewer.Offset.WithY(y);
                        }
                        tree.InvalidateVisual();
                    }, TimeSpan.FromMilliseconds(1));
                }
                
            }, DispatcherPriority.Render);
        });
    }
    
    protected override void DrawRow(Typeface typeface, Pen pen, IBrush foreground, DrawingContext context, object? row, Rect rect)
    {
        {
            var ft = new FormattedText(row?.ToString() ?? "(null)", CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, 12, foreground)
            {
                MaxTextWidth = float.MaxValue,
                MaxTextHeight = RowHeight
            };
            
            var isHover = mouseOverRow == row;
            var isSelected = SelectedNode == row;
        
            if (isHover || isSelected)
                context.DrawRectangle(isHover ? HoverRowBackground : SelectedRowBackground, null, rect);

            var nestLevel = (row is SpawnGroup a1 ? a1.NestLevel : ((SpawnInstance)row!).NestLevel);
            var iconRect = new Rect(Indent * nestLevel, rect.Center.Y - 16 / 2, 16, 16);
            var textOrigin = new Point(Indent * nestLevel + 18, rect.Y + rect.Height / 2 - ft.Height / 2);
            
            if (row is SpawnGroup group)
            {
                IImage? icon = null;
                switch (group.Type, group.IsExpanded)
                {
                    case (GroupType.Map, _):
                        icon = mapIcon;
                        break;
                    case (GroupType.Area, false):
                    case (GroupType.Zone, false):
                        icon = zoneAreaIcon;
                        break;
                    case (GroupType.Area, true):
                    case (GroupType.Zone, true):
                        icon = zoneAreaIconOpened;
                        break;
                    case (GroupType.SpawnGroup, _):
                        icon = spawnGroupIcon;
                        break;
                    case (GroupType.Creature, _):
                        icon = creaturesIcon;
                        break;
                    case (GroupType.GameObject, _):
                        icon = gobjectsIcon;
                        break;
                }
                if (icon != null)
                    context.DrawImage(icon, iconRect);
                context.DrawText(ft, textOrigin);
                DrawToggleMark(rect.WithWidth(RowHeight).WithX(Indent * (group.NestLevel - 1)), context, pen, group.IsExpanded);
            }
            else if (row is SpawnInstance spawn)
            {
                if (creatureIcon != null && gobjectIcon != null)
                    context.DrawImage(spawn is CreatureSpawnInstance ? creatureIcon : gobjectIcon, iconRect);

                context.DrawText(ft, textOrigin);
                double x = rect.Width - 2;
                if (spawn.IsSpawned)
                    context.DrawRectangle(foreground, null, new Rect(x - 6, rect.Center.Y - 3, 6, 6), 3, 3);
            }
        }
    }
}