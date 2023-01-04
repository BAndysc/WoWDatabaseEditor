using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace AvaloniaStyles.Controls;

public class ToolbarControl : TemplatedControl
{
    public ToolbarControl() => this.Children.CollectionChanged += new NotifyCollectionChangedEventHandler(this.ChildrenChanged);
    
    public static readonly StyledProperty<bool> IsOverflowProperty = AvaloniaProperty.Register<ToolbarControl, bool>(nameof(IsOverflow));
    
    public bool IsOverflow
    {
        get => GetValue(IsOverflowProperty);
        set => SetValue(IsOverflowProperty, value);
    }
    
    private Panel? childrenHost;
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        childrenHost = e.NameScope.Find<Panel>("PART_ChildrenHost");
        if (childrenHost == null)
            throw new NullReferenceException("Couldn't find PART_ChildrenHost in ToolbarControl Template");
        foreach (var child in Children)
        {
            childrenHost.Children.Add(child);
            child.ApplyStyling();
        }
    }
    
    private void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                List<Control> list = e.NewItems!.OfType<Control>().ToList();
                if (childrenHost != null)
                {
                    foreach (Control control in list)
                        childrenHost.Children.Add(control);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                List<Control> oldList = e.OldItems!.OfType<Control>().ToList();
                if (childrenHost != null)
                {
                    foreach (Control control in oldList)
                        childrenHost.Children.Remove(control);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                for (int index1 = 0; index1 < e.OldItems!.Count; ++index1)
                {
                    int index2 = index1 + e.OldStartingIndex;
                    Control? newItem = (Control?) e.NewItems![index1];
                    if (childrenHost != null && newItem != null)
                        childrenHost.Children[index2] = newItem;
                }
                break;
            case NotifyCollectionChangedAction.Move:
                if (childrenHost != null)
                {
                    childrenHost.Children.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                throw new NotSupportedException();
        }
    }
    
    [Content]
    public Avalonia.Controls.Controls Children { get; } = new Avalonia.Controls.Controls();
}