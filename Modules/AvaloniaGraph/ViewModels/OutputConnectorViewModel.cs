using System.Collections.ObjectModel;
using Avalonia.Media;
using AvaloniaGraph.Controls;

namespace AvaloniaGraph.ViewModels;

public class OutputConnectorViewModel<T> : ConnectorViewModel<T> where T : NodeViewModelBase<T>
{
    public OutputConnectorViewModel(T node, ConnectorAttachMode attachMode, string name, Color color) : base(node, attachMode, name, color)
    {
        Connections = new ObservableCollection<ConnectionViewModel<T>>();
        Connections.CollectionChanged += (e, w) => { RaisePropertyChanged(nameof(NonEmpty)); };
    }

    public override ConnectorDirection ConnectorDirection => ConnectorDirection.Output;

    public ObservableCollection<ConnectionViewModel<T>> Connections { get; }

    public override bool NonEmpty => Connections.Count != 0;
}