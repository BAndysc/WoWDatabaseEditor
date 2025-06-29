using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using DynamicData.Binding;
using WDE.MVVM;

namespace WDE.QuestChainEditor.ViewModels;

public class ConnectorViewModel : ObservableBase
{
    public ConnectorViewModel(BaseQuestViewModel node)
    {
        Node = node;
        Connections = new ObservableCollectionExtended<ConnectionViewModel>();
        Connections.CollectionChanged += (e, w) =>
        {
            RaisePropertyChanged(nameof(NonEmpty));
            RaisePropertyChanged(nameof(Empty));
        };
    }

    public ObservableCollectionExtended<ConnectionViewModel> Connections { get; }

    public bool NonEmpty => Connections.Count(c => c.RequirementType != ConnectionType.FactionChange) > 0;

    public bool Empty => !NonEmpty;

    public BaseQuestViewModel Node { get; }

    private Point anchor;
    public Point Anchor
    {
        get => anchor;
        set => SetProperty(ref anchor, value);
    }
}