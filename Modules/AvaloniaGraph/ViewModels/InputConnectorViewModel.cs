using System.Collections.ObjectModel;
using Avalonia.Media;
using AvaloniaGraph.Controls;

namespace AvaloniaGraph.ViewModels;

public class InputConnectorViewModel<T, K> : ConnectorViewModel<T, K> where T : NodeViewModelBase<T, K> where K : ConnectionViewModel<T, K>
{
    public InputConnectorViewModel(T node, ConnectorAttachMode attachMode, string name, Color color) : base(node, attachMode, name, color)
    {
        Connections = new ObservableCollection<K>();
        Connections.CollectionChanged += (e, w) => { RaisePropertyChanged(nameof(NonEmpty)); };
    }

    public override ConnectorDirection ConnectorDirection => ConnectorDirection.Input;

    public ObservableCollection<K> Connections { get; }

    public override bool NonEmpty => Connections.Count > 0;
}