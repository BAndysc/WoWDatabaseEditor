using System;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Styling;

namespace AvaloniaGraph.Controls;

public class NodesContainer : ListBox, IStyleable
{
    Type IStyleable.StyleKey => typeof(ListBox);

    // Avalonia 11
    // protected override Control CreateContainerForItemOverride()
    // {
    //     return new GraphNodeItemView();
    // }
}