using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Processing.Processors.Paths;

[NonUniqueProvider]
public interface ISniffWaypointsExporter
{
    string Id { get; }
    string Name { get; }

    Task Export(StringBuilder sb, int basePathNum, UniversalGuid guid, IWaypointProcessor.UnitMovementState state, float randomness);
}