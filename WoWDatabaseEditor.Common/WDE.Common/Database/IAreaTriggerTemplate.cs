namespace WDE.Common.Database
{
    public interface IAreaTriggerTemplate
    {
        public uint Id { get; }
        bool IsServerSide { get; }
        string? Name { get; }
        string? ScriptName { get; }
        string? AIName { get; }
    }
    
    public class AbstractAreaTriggerTemplate : IAreaTriggerTemplate
    {
        public uint Id { get; init; }
        public bool IsServerSide { get; init; }
        public string? Name { get; init; }
        public string? ScriptName { get; init; }
        public string? AIName { get; init; }
    }
}