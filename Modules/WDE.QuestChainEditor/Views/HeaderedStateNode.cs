using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Nodify;
using Nodify.Compatibility;
using WDE.QuestChainEditor.ViewModels;

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

    public override void OnConnectorDragStarted(MouseButtonEventArgs e)
    {
      if (e.KeyModifiers == 0 &&
          DataContext is QuestViewModel q && q.ExclusiveGroup != null && Editor is {} editor &&
          editor.ContainerFromItem(q.ExclusiveGroup) is { } groupContainer &&
          groupContainer.FindDescendantOfType<Connector>() is { } groupConnector)
      {
        if (editor.DataContext is QuestChainDocumentViewModel vm)
          vm.ShowShiftKeyTeachingTip();
        // a hack, very manual way to redirect connection from a different node
        ReleaseMouseCapture();
        PropagateMouseCapturedWithin(false);
        e.Capture(groupConnector);
        PropagateMouseCapturedWithin(true);
        groupConnector.OnConnectorDragStarted(e);
      }
      else
        base.OnConnectorDragStarted(e);
    }
}