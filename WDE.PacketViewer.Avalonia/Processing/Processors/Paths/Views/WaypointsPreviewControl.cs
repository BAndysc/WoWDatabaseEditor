using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WDE.Common.Utils;
using WDE.PacketViewer.Processing.Processors.Paths.ViewModels;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Avalonia.Processing.Processors.Paths.Views;

public class WaypointsPreviewControl : Control
{
    public static readonly StyledProperty<CreatureGuidViewModel?> CreatureProperty = AvaloniaProperty.Register<WaypointsPreviewControl, CreatureGuidViewModel?>(nameof(Creature));
    public static readonly StyledProperty<ITableMultiSelection?> SelectionProperty = AvaloniaProperty.Register<WaypointsPreviewControl, ITableMultiSelection?>(nameof(Selection));

    static WaypointsPreviewControl()
    {
        AffectsRender<WaypointsPreviewControl>(CreatureProperty);
        CreatureProperty.Changed.AddClassHandler<WaypointsPreviewControl>((control, e) =>
        {
            control.UnbindCreature();
            if (e.NewValue is CreatureGuidViewModel creature)
                control.BindCreature(creature);
        });
    }
    
    public override void Render(DrawingContext context)
    {
        if (Creature is not { } creature)
            return;

        Vector3 minimum = new(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maximum = new(float.MinValue, float.MinValue, float.MinValue);
        
        foreach (var point in creature.Paths.SelectMany(x => x.Waypoints))
        {
            minimum = Vector3.Min(minimum, new Vector3(point.X, point.Y, point.Z));
            maximum = Vector3.Max(maximum, new Vector3(point.X, point.Y, point.Z));
        }
        
        var size = maximum - minimum;

        float originalAspectRatio = size.X / size.Y;
        float boundsAspectRatio = (float)Bounds.Width / (float)Bounds.Height;
        
        float scaleX, scaleY;

        if (originalAspectRatio > boundsAspectRatio) 
        {
            // Scale by width
            scaleX = (float)Bounds.Width / size.X;
            scaleY = scaleX;
        }
        else 
        {
            // Scale by height
            scaleY = (float)Bounds.Height / size.Y;
            scaleX = scaleY;
        }
        
        Point WorldToLocal(Vector3 world)
        {
            // Apply the scale factors
            float localX = (world.X - minimum.X) * scaleX;
            float localY = (world.Y - minimum.Y) * scaleY;

            // Center the result within the 2D bounds
            float offsetX = ((float)Bounds.Width - size.X * scaleX) / 2;
            float offsetY = ((float)Bounds.Height - size.Y * scaleY) / 2;

            return new Point(localX + offsetX, localY + offsetY);
        }
        
        var pen = new Pen(Brushes.White, 1);

        foreach (var path in Creature.Paths)
        {
            if (!path.IsVisible)
                continue;
            
            Vector3? prevPoint = null;
            foreach (var point in path.Waypoints)
            {
                var local = WorldToLocal(new Vector3(point.X, point.Y, point.Z));
                if (prevPoint != null)
                {
                    context.DrawLine(pen, WorldToLocal(prevPoint.Value), local);
                }
                context.DrawEllipse(Brushes.White, pen, local, 2, 2);
                prevPoint = new Vector3(point.X, point.Y, point.Z);
            }
        }

        if (Selection is { } selection)
        {
            var itr = selection.ContainsIterator;
            for (var pathIndex = 0; pathIndex < Creature.Paths.Count; pathIndex++)
            {
                var path = Creature.Paths[pathIndex];
                for (var pointIndex = 0; pointIndex < path.Waypoints.Count; pointIndex++)
                {
                    if (!itr.Contains(new VerticalCursor(pathIndex, pointIndex)))
                        continue;

                    var point = path.Waypoints[pointIndex];
                    var p = new Vector3(point.X, point.Y, point.Z);
                    
                    var local = WorldToLocal(p);
                    context.DrawEllipse(Brushes.Red, pen, local, 5, 5);
                }
            }
        }
    }
    
    private ITableMultiSelection? boundSelection;
    private CreatureGuidViewModel? boundCreature;
    private List<CreaturePathViewModel> boundPaths = new();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (boundSelection != null)
        {
            boundSelection.SelectionChanged -= OnSelectionChanged;
            boundSelection = null;
        }

        UnbindCreature();

        if (Selection is { } selection)
        {
            selection.SelectionChanged += OnSelectionChanged;
            boundSelection = selection;
        }

        if (Creature is { } creature)
        {
            BindCreature(creature);
        }
    }

    private void UnbindCreature()
    {
        if (boundCreature != null)
        {
            boundCreature.Paths.CollectionChanged -= OnPathChanged;
            foreach (CreaturePathViewModel n in boundPaths)
            {
                n.PropertyChanged -= OnPathPropertyChanged;
            }

            boundPaths.Clear();
            boundCreature = null;
        }
    }

    private void BindCreature(CreatureGuidViewModel creature)
    {
        creature.Paths.CollectionChanged += OnPathChanged;
        foreach (CreaturePathViewModel n in creature.Paths)
        {
            n.PropertyChanged += OnPathPropertyChanged;
            boundPaths.Add(n);
        }

        boundCreature = creature;
    }

    private void OnPathChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (CreaturePathViewModel n in e.NewItems)
            {
                n.PropertyChanged += OnPathPropertyChanged;
                boundPaths.Add(n);
            }
        }
        if (e.OldItems != null)
        {
            foreach (CreaturePathViewModel o in e.OldItems)
            {
                o.PropertyChanged -= OnPathPropertyChanged;
                boundPaths.Remove(o);
            }
        }
    }

    private void OnPathPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OnSelectionChanged()
    {
        InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (boundSelection != null)
        {
            boundSelection.SelectionChanged -= OnSelectionChanged;
            boundSelection = null;
        }
    }

    public CreatureGuidViewModel? Creature
    {
        get => GetValue(CreatureProperty);
        set => SetValue(CreatureProperty, value);
    }

    public ITableMultiSelection? Selection
    {
        get => GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }
}