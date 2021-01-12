namespace WDE.Common.Database
{
    public interface ICreatureTemplate
    {
        uint Entry { get; set; }
        string Name { get; set; }
        string AIName { get; set; }
        string ScriptName { get; set; }
    }
}