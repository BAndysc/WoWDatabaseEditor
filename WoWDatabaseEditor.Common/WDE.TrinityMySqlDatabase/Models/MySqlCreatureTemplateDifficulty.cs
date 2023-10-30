using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models;

[Table(Name = "creature_template_difficulty")]
public class MySqlCreatureTemplateDifficulty : ICreatureTemplateDifficulty
{
    [PrimaryKey]
    [Column(Name = "Entry")]
    public uint Entry { get; set; }
    
    [PrimaryKey]
    [Column(Name = "DifficultyID")]
    public uint DifficultyId { get; set; }

    [Column(Name = "LootID")]
    public uint LootId { get; set; }
    
    [Column(Name = "SkinLootID")]
    public uint SkinningLootId { get; set; }
    
    [Column(Name = "PickPocketLootID")]
    public uint PickpocketLootId { get; set; }
}