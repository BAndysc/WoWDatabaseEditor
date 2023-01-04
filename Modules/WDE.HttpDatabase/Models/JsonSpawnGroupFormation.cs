using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;

public class JsonSpawnGroupFormation : ISpawnGroupFormation
{
    public uint Id { get; set; }
    public FormationShape FormationType { get; set; }
    public float Spread { get; set; }
    public int Options { get; set; }
    public int PathId { get; set; }
    public MovementType MovementType { get; set; }
    public string? Comment { get; set; }
}