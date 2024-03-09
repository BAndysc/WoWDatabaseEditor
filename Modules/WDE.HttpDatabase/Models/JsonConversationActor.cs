using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;

public class JsonConversationActor : IConversationActor
{
    public uint ConversationId { get; set; }
    public uint ConversationActorId { get; set; }
    public uint Idx { get; set; }
    public int CreatureId { get; set; }
}