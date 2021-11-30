namespace WDE.Common.Database
{
    public interface ICreatureTemplate
    {
        uint Entry { get; set; }
        public uint ModelId1 { get; set; }
        public uint ModelId2 { get; set; }
        public uint ModelId3 { get; set; }
        public uint ModelId4 { get; set; }
        uint Scale { get; set; }
        uint GossipMenuId { get; set; }
        string Name { get; set; }
        string AIName { get; set; }
        string ScriptName { get; set; }
        
        int ModelsCount { get; }
        uint GetModel(int index);
    }
}