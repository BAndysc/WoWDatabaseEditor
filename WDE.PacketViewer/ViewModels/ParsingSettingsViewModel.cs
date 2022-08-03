using PropertyChanged.SourceGenerator;
using WDE.MVVM;
using WDE.PacketViewer.Processing;

namespace WDE.PacketViewer.ViewModels;

public partial class ParsingSettingsViewModel : ObservableBase, IParsingSettings
{
    [Notify] private bool translateChatToEnglish;
    [Notify] private WaypointsDumpType waypointsDumpType;
}