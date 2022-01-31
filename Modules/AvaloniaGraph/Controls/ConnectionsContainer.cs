using System;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Styling;

namespace AvaloniaGraph.Controls;

public class ConnectionsContainer : ListBox, IStyleable
{
    Type IStyleable.StyleKey => typeof(ListBox);

    protected override IItemContainerGenerator CreateItemContainerGenerator()
    {
        return new ItemContainerGenerator<ConnectionItem>(
            this,
            ContentControl.ContentProperty,
            ContentControl.ContentTemplateProperty);
    }
}