using AvaloniaGraph.ViewModels;

namespace WDE.QuestChainEditor.ViewModels;

public class QuestConnectionViewModel : ConnectionViewModel<QuestViewModel, QuestConnectionViewModel>
{
    public QuestConnectionViewModel(OutputConnectorViewModel<QuestViewModel, QuestConnectionViewModel> from, InputConnectorViewModel<QuestViewModel, QuestConnectionViewModel> to) : base(from, to)
    {
    }

    public QuestConnectionViewModel(InputConnectorViewModel<QuestViewModel, QuestConnectionViewModel> to) : base(to)
    {
    }

    public QuestConnectionViewModel(OutputConnectorViewModel<QuestViewModel, QuestConnectionViewModel> from) : base(from)
    {
    }
}