namespace WDE.Common.Database
{
    public interface IConversationTemplate
    {
        public uint Id { get; }
        public uint FirstLineId { get; }
        public string ScriptName { get; }
    }

    public struct AbstractConversationTemplate : IConversationTemplate
    {
        public uint Id { get; init; }
        public uint FirstLineId { get; init; }
        public string ScriptName { get; init; }
    }
}