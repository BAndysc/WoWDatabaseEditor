using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models;

[Table(Name = "conversation_actors")]
public class MySqlConversationActor : IConversationActor
{
    [Column(Name = "ConversationId")]
    public uint ConversationId { get; set; }

    [Column(Name = "ConversationActorId")]
    public uint ConversationActorId { get; set; }

    [Column(Name = "Idx")]
    public uint Idx { get; set; }

    [Column(Name = "CreatureId")]
    public int CreatureId { get; set; }
}