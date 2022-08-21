using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "creature_ai_summons")]
public class CreatureAiSummon : ICreatureAiSummon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "id")]
    public uint Id { get; set; }

    [Column(Name = "position_x")]
    public float X { get; set; }
    
    [Column(Name = "position_y")]
    public float Y { get; set; }
    
    [Column(Name = "position_z")]
    public float Z { get; set; }
    
    [Column(Name = "orientation")]
    public float O { get; set; }
    
    [Column(Name = "spawntimesecs")]
    public uint SpawnTimeSeconds { get; set; }
    
    [Column(Name = "comment")]
    public string Comment { get; set; } = "";
}