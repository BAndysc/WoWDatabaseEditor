using System.Collections.Generic;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors.Paths;

namespace WDE.PacketViewer.Processing;

[UniqueProvider]
public interface IParsingSettings
{
    bool TranslateChatToEnglish { get; set; }
    ISniffWaypointsExporter? WaypointsExporter { get; set; }
    IList<ISniffWaypointsExporter> Exporters { get; }
}