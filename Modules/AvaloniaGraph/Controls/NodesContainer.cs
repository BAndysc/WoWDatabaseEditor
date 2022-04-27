using System;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Styling;

namespace AvaloniaGraph.Controls;

public class NodesContainer : ListBox, IStyleable
{
    Type IStyleable.StyleKey => typeof(ListBox);

    protected override IItemContainerGenerator CreateItemContainerGenerator()
    {
        return new ItemContainerGenerator<GraphNodeItemView>(
            this,
            ContentControl.ContentProperty,
            ContentControl.ContentTemplateProperty);
    }
}