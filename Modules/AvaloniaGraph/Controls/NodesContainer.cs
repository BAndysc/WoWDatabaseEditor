using System;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Styling;

namespace AvaloniaGraph.Controls;

public class NodesContainer : ListBox, IStyleable
{
    Type IStyleable.StyleKey => typeof(ListBox);
    
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new GraphNodeItemView();
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        return this.NeedsContainer<GraphNodeItemView>(item, out recycleKey);
    }
}