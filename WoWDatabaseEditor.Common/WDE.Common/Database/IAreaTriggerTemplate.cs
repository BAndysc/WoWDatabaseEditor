namespace WDE.Common.Database
{
    public interface IAreaTriggerTemplate
    {
        public uint Id { get; }
        bool IsServerSide { get; }
        string ScriptName { get; }
    }
}