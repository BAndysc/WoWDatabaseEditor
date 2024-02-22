namespace WDE.Common.DBC.Structs;

public interface IConversationLine
{
    uint Id { get; }
    uint BroadcastTextId { get; }
    uint NextLineId { get; }
}

public class ConversationLine : IConversationLine
{
    public uint Id { get; init; }
    public uint BroadcastTextId { get; init; }
    public uint NextLineId { get; init; }
}