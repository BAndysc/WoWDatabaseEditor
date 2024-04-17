using System.Collections.ObjectModel;
using Avalonia;
using WDE.MVVM;

namespace WDE.QuestChainEditor.ViewModels;

public class ConnectorViewModel : ObservableBase
{
    public ConnectorViewModel(BaseQuestViewModel node)
    {
        Node = node;
        Connections = new ObservableCollection<ConnectionViewModel>();
        Connections.CollectionChanged += (e, w) => { RaisePropertyChanged(nameof(NonEmpty)); };
    }

    public ObservableCollection<ConnectionViewModel> Connections { get; }

    public bool NonEmpty => Connections.Count != 0;

    public bool Empty => Connections.Count == 0;

    public BaseQuestViewModel Node { get; }

    private Point anchor;
    public Point Anchor
    {
        get => anchor;
        set => SetProperty(ref anchor, value);
    }
}