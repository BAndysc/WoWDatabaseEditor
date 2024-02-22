namespace WDE.Common.Database;

public interface IConversationActor
{
    uint ConversationId { get; }
    uint ConversationActorId { get; }
    uint Idx { get; }
    int CreatureId { get; }
}