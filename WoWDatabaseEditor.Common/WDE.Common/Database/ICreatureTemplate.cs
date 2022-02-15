namespace WDE.Common.Database
{
    public interface ICreatureTemplate
    {
        uint Entry { get; set; }
        float Scale { get; set; }
        uint GossipMenuId { get; set; }
        string Name { get; set; }
        string AIName { get; set; }
        string ScriptName { get; set; }
        
        int ModelsCount { get; }
        uint GetModel(int index);
    }
}