using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace AvaloniaStyles.Controls;

public class ToolbarItemsControl : ItemsControl
{
    public static readonly StyledProperty<bool> IsOverflowProperty = AvaloniaProperty.Register<ToolbarItemsControl, bool>(nameof(IsOverflow));
    
    public bool IsOverflow
    {
        get => GetValue(IsOverflowProperty);
        set => SetValue(IsOverflowProperty, value);
    }
}

public class ToolbarControl : TemplatedControl
{
    public static readonly StyledProperty<bool> IsOverflowProperty = AvaloniaProperty.Register<ToolbarControl, bool>(nameof(IsOverflow));
    
    public bool IsOverflow
    {
        get => GetValue(IsOverflowProperty);
        set => SetValue(IsOverflowProperty, value);
    }
    
    private Panel? panel;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        panel = e.NameScope.Get<ToolbarPanel>("PART_Panel");
        panel.Children.AddRange(Children);
    }
    
    public ToolbarControl() => Children.CollectionChanged += ChildrenChanged;

    private void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (panel == null)
            return;

        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            panel.Children.AddRange(e.NewItems!.OfType<Control>());
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            panel.Children.RemoveAll(e.OldItems!.OfType<Control>());
        }
        else if (e.Action == NotifyCollectionChangedAction.Replace)
        {
            for (int index1 = 0; index1 < e.OldItems!.Count; ++index1)
            {
                int index2 = index1 + e.OldStartingIndex;
                Control? newItem = (Control?) e.NewItems![index1];
                panel.Children[index2] = newItem!;
            }
        }
        else
        {
            if (e.Action != NotifyCollectionChangedAction.Reset)
                return;
            panel.Children.Clear();
            panel.Children.AddRange(Children);
        }
    }
    
    [Content]
    public Avalonia.Controls.Controls Children { get; } = new();
}