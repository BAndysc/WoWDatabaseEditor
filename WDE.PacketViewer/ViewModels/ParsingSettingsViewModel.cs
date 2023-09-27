using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using PropertyChanged.SourceGenerator;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.PacketViewer.Processing;
using WDE.PacketViewer.Processing.Processors.Paths;
using WDE.PacketViewer.Settings;

namespace WDE.PacketViewer.ViewModels;

[AutoRegister]
[SingleInstance]
public partial class ParsingSettingsViewModel : ObservableBase, IParsingSettings
{
    private readonly IPacketViewerSettings packetViewerSettings;
    [Notify] private bool translateChatToEnglish;
    [Notify] private ISniffWaypointsExporter? waypointsExporter;
    
    public IList<ISniffWaypointsExporter> Exporters { get; }

    public ParsingSettingsViewModel(IEnumerable<ISniffWaypointsExporter> exporters,
        IPacketViewerSettings packetViewerSettings)
    {
        this.packetViewerSettings = packetViewerSettings;
        Exporters = exporters.ToList();
        waypointsExporter = Exporters.FirstOrDefault(x => x.Id == this.packetViewerSettings.Settings.DefaultWaypointExporterId)
                            ?? Exporters.FirstOrDefault();

        this.ToObservable(x => x.WaypointsExporter)
            .Skip(1)
            .SubscribeAction(x =>
            {
                if (x == null)
                    return;

                this.packetViewerSettings.Settings = this.packetViewerSettings.Settings with
                {
                    DefaultWaypointExporterId = x.Id
                };
            });
    }
}