namespace WDE.Common.Database
{
    public interface IGameObjectTemplate
    {
        uint Entry { get; set; }
        uint DisplayId { get; set; }
        float Size { get; set; }
        string Name { get; set; }
        string AIName { get; set; }
        string ScriptName { get; set; }
    }
}