using System.Collections.ObjectModel;
using Avalonia.Media;
using AvaloniaGraph.Controls;

namespace AvaloniaGraph.ViewModels;

public class InputConnectorViewModel<T> : ConnectorViewModel<T> where T : NodeViewModelBase<T>
{
    public InputConnectorViewModel(T node, ConnectorAttachMode attachMode, string name, Color color) : base(node, attachMode, name, color)
    {
        Connections = new ObservableCollection<ConnectionViewModel<T>>();
        Connections.CollectionChanged += (e, w) => { RaisePropertyChanged(nameof(NonEmpty)); };
    }

    public override ConnectorDirection ConnectorDirection => ConnectorDirection.Input;

    public ObservableCollection<ConnectionViewModel<T>> Connections { get; }

    public override bool NonEmpty => Connections.Count > 0;
}