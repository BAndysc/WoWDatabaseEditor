using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;

public class JsonConversationActorTemplate : IConversationActorTemplate
{
    public uint Id { get; set; }
    public uint CreatureId { get; set; }
}