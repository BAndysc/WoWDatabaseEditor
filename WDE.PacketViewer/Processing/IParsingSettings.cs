namespace WDE.PacketViewer.Processing;

public interface IParsingSettings
{
    bool TranslateChatToEnglish { get; }
    WaypointsDumpType WaypointsDumpType { get; }
}

public enum WaypointsDumpType
{
    Text,
    SmartWaypoints
}