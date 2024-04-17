using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Nodify;

namespace WDE.QuestChainEditor.Views;

public class HeaderedStateNode : StateNode
{
    public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<HeaderedStateNode, object?>(nameof (Header));
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty = AvaloniaProperty.Register<HeaderedStateNode, IDataTemplate?>(nameof (HeaderTemplate));

    static HeaderedStateNode() => HeaderProperty.Changed.AddClassHandler<HeaderedStateNode>((x, e) => x.HeaderChanged(e));

    public object? Header
    {
      get => GetValue(HeaderProperty);
      set => SetValue(HeaderProperty, value);
    }

    public IDataTemplate? HeaderTemplate
    {
      get => GetValue(HeaderTemplateProperty);
      set => SetValue(HeaderTemplateProperty, value);
    }

    private void HeaderChanged(AvaloniaPropertyChangedEventArgs e)
    {
      if (e.OldValue is ILogical oldValue)
        this.LogicalChildren.Remove(oldValue);
      if (!(e.NewValue is ILogical newValue))
        return;
      this.LogicalChildren.Add(newValue);
    }
}