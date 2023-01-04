using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace WDE.Common.Avalonia.Controls;

public class VirtualizedTreeViewItem : TemplatedControl, ISelectable
{
    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<VirtualizedTreeViewItem, IDataTemplate?>(nameof(ContentTemplate));
    
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<VirtualizedTreeViewItem, bool>(nameof(IsSelected));

    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }
    
    static VirtualizedTreeViewItem()
    {
        SelectableMixin.Attach<VirtualizedTreeViewItem>(IsSelectedProperty);
        PressedMixin.Attach<VirtualizedTreeViewItem>();
        FocusableProperty.OverrideDefaultValue<VirtualizedTreeViewItem>(true);
    }
    
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}