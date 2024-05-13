using System;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models.Classic;

[Table(Name = "creature_template")]
public class CreatureTemplateClassic : ICreatureTemplate
{
    [Identity]
    [PrimaryKey]
    [Column("Entry")]
    public uint Entry { get; set; }

    public uint DifficultyEntry1 => 0;

    public uint DifficultyEntry2 => 0;

    public uint DifficultyEntry3 => 0;

    [Column("Scale")]
    public float Scale { get; set; }

    [Column("GossipMenuId")]
    public uint GossipMenuId { get; set; }

    [Column("MinLevel")]
    public short MinLevel { get; set; }

    [Column("MaxLevel")]
    public short MaxLevel { get; set; }

    [Column("Name")]
    public string Name { get; set; } = "";
    
    [Column(Name = "SubName")]
    public string? SubName { get; set; } = "";

    public string? IconName { get; set; }

    [Column("AIName")] 
    public string AIName { get; set; } = "";

    [Column("ScriptName")]
    public string ScriptName { get; set; } = "";

    [Column("UnitFlags")]
    public GameDefines.UnitFlags UnitFlags { get; set; }

    public GameDefines.UnitFlags2 UnitFlags2 => 0;
    
    [Column(Name = "SpeedWalk")]
    public float SpeedWalk { get; set; }
        
    [Column(Name = "SpeedRun")]
    public float SpeedRun { get; set; }

    [Column("NpcFlags")]
    public GameDefines.NpcFlags NpcFlags { get; set; }

    [Column("DisplayId1")]
    public uint ModelId1               { get; set; }

    [Column("DisplayId2")]
    public uint ModelId2               { get; set; }
    
    [Column("DisplayId3")]
    public uint ModelId3               { get; set; }
    
    [Column("DisplayId4")]
    public uint ModelId4               { get; set; }

    [Column("EquipmentTemplateId")]
    public uint? EquipmentTemplateId { get; set; }

    public short RequiredExpansion { get; set; }
    public byte Rank { get; set; }
    public byte UnitClass { get; set; }
    public int Family { get; set; }
    public byte Type { get; set; }
    public uint TypeFlags { get; set; }
    public uint VehicleId { get; set; }
    public float HealthMod { get; set; }
    public float ManaMod { get; set; }
    public bool RacialLeader { get; set; }
    public uint MovementId { get; set; }
    public uint KillCredit1 { get; set; }
    public uint KillCredit2 { get; set; }

    [Column(Name = "InhabitType")]
    public GameDefines.InhabitType InhabitType { get; set; }

    [Column(Name = "ExtraFlags")]
    public uint FlagsExtra { get; set; }

    [Column(Name = "Faction")]
    public uint FactionTemplate { get; set; }
    
    public int ModelsCount => 4;
    public uint GetModel(int index)
    {
        switch (index)
        {
            case 0:
                return ModelId1;
            case 1:
                return ModelId2;
            case 2:
                return ModelId3;
            case 3:
                return ModelId4;
        }

        throw new Exception("Model out of range");
    }
    
    [Column("LootId")] 
    public uint LootId { get; set; }
    
    [Column("PickpocketLootId")] 
    public uint PickpocketLootId { get; set; }
    
    [Column("SkinningLootId")] 
    public uint SkinningLootId { get; set; }
    
    public int LootCount => 1;
        
    public uint GetLootId(int index) => LootId;
}