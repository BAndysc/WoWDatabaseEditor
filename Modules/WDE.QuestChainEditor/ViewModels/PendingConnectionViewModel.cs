using Avalonia;
using PropertyChanged.SourceGenerator;
using WDE.MVVM;

namespace WDE.QuestChainEditor.ViewModels;

public partial class PendingConnectionViewModel : ObservableBase
{
    [Notify] private bool isVisible;
    [Notify] private Point targetLocation;

    [Notify] private BaseQuestViewModel? from;
    [Notify] private BaseQuestViewModel? to;
}