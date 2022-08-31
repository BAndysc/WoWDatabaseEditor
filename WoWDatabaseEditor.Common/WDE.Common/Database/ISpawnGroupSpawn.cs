namespace WDE.Common.Database;

public interface ISpawnGroupSpawn
{
    uint TemplateId { get; }
    uint Guid { get; }
    SpawnGroupTemplateType Type { get; }
    int? SlotId { get; }
    uint? Chance { get; }
}

public class AbstractSpawnGroupSpawn : ISpawnGroupSpawn
{
    public uint TemplateId { get; set; }
    public uint Guid { get; set; }
    public SpawnGroupTemplateType Type { get; set; }
    public int? SlotId { get; set; }
    public uint? Chance { get; set; }
}
