namespace WDE.Common.Database
{
    public interface IGameObjectTemplate
    {
        uint Entry { get; }
        uint DisplayId { get; }
        float Size { get; }
        public uint FlagsExtra => 0;
        string Name { get; }
        string AIName { get; }
        string ScriptName { get; }
    }
}