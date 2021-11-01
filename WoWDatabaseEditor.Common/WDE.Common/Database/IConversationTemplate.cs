namespace WDE.Common.Database
{
    public interface IConversationTemplate
    {
        public uint Id { get; }
        public uint FirstLineId { get; }
        public string ScriptName { get; }
    }
}