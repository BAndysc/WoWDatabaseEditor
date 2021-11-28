namespace WDE.Common.Database
{
    public interface ICreatureTemplate
    {
        uint Entry { get; set; }
        uint modelid1 { get; set; }
        // uint modelid2 { get; set; }
        // uint modelid3 { get; set; }
        // uint modelid4 { get; set; }
        uint scale { get; set; }
        uint GossipMenuId { get; set; }
        string Name { get; set; }
        string AIName { get; set; }
        string ScriptName { get; set; }
    }
}