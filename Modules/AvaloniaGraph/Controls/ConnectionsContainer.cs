using System;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Styling;

namespace AvaloniaGraph.Controls;

public class ConnectionsContainer : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new ConnectionItem();
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        return this.NeedsContainer<ConnectionItem>(item, out recycleKey);
    }
}