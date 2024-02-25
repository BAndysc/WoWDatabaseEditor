using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "creature_template")]
    public class MySqlCreatureTemplateWrath : ICreatureTemplate
    {
        [PrimaryKey]
        [Identity]
        [Column(Name = "entry")]
        public uint Entry { get; set; }

        [Column(Name = "difficulty_entry_1")]
        public uint DifficultyEntry1 { get; set; }

        [Column(Name = "difficulty_entry_2")]
        public uint DifficultyEntry2 { get; set; }

        [Column(Name = "difficulty_entry_3")]
        public uint DifficultyEntry3 { get; set; }

        [Column(Name = "modelid1")]
        public uint ModelId1 { get; set; }

        [Column(Name = "modelid2")]
        public uint ModelId2 { get; set; }

        [Column(Name = "modelid3")]
        public uint ModelId3 { get; set; }

        [Column(Name = "modelid4")]
        public uint ModelId4 { get; set; }

        [Column(Name = "scale")]
        public float Scale { get; set; }

        [Column(Name = "gossip_menu_id")] 
        public uint GossipMenuId { get; set; }

        [Column(Name = "minlevel")]
        public short MinLevel { get; set; }
        
        [Column(Name = "maxlevel")]
        public short MaxLevel { get; set; }

        [Column(Name = "name")] 
        public string Name { get; set; } = "";
        
        [Column(Name = "subname")]
        public string? SubName { get; set; } = "";
        
        [Column(Name = "AIName")]
        public string AIName { get; set; } = "";

        [Column(Name = "ScriptName")]
        public string ScriptName { get; set; } = "";
        
        [Column(Name = "unit_flags")]
        public GameDefines.UnitFlags UnitFlags { get; set; }

        [Column(Name = "unit_flags2")]
        public GameDefines.UnitFlags2 UnitFlags2 { get; set; }

        [Column(Name = "npcflag")]
        public GameDefines.NpcFlags NpcFlags { get; set; }
        
        [Column(Name = "speed_walk")]
        public float SpeedWalk { get; set; }
        
        [Column(Name = "speed_run")]
        public float SpeedRun { get; set; }

        [Column(Name = "faction")]
        public uint FactionTemplate { get; set; }
        
        public uint? EquipmentTemplateId => null;
        
        [Column(Name = "IconName")] 
        public string? IconName { get; set; }

        [Column(Name = "exp")] 
        public short RequiredExpansion { get; set; }
        
        [Column(Name = "rank")] 
        public byte Rank { get; set; }
        
        [Column(Name = "unit_class")] 
        public byte UnitClass { get; set; }
        
        [Column(Name = "family")] 
        public int Family { get; set; }
        
        [Column(Name = "type")] 
        public byte Type { get; set; }
        
        [Column(Name = "type_flags")] 
        public uint TypeFlags { get; set; }
        
        [Column(Name = "VehicleId")] 
        public uint VehicleId { get; set; }
        
        [Column(Name = "HealthModifier")] 
        public float HealthMod { get; set; }
        
        [Column(Name = "ManaModifier")] 
        public float ManaMod { get; set; }
        
        [Column(Name = "RacialLeader")] 
        public bool RacialLeader { get; set; }
        
        [Column(Name = "movementId")] 
        public uint MovementId { get; set; }
        
        [Column(Name = "KillCredit1")] 
        public uint KillCredit1 { get; set; }
        
        [Column(Name = "KillCredit2")] 
        public uint KillCredit2 { get; set; }

        [Column(Name = "flags_extra")]
        public uint FlagsExtra { get; set; }
        
        public GameDefines.InhabitType InhabitType => 0;
        
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
        
        [Column("lootid")] 
        public uint LootId { get; set; }
    
        [Column("pickpocketloot")] 
        public uint PickpocketLootId { get; set; }
    
        [Column("skinloot")] 
        public uint SkinningLootId { get; set; }
    
        public int LootCount => 1;
        
        public uint GetLootId(int index) => LootId;
    }
    
    [Table(Name = "creature_template")]
    public class MySqlCreatureTemplateMaster : ICreatureTemplate
    {
        [PrimaryKey]
        [Identity]
        [Column(Name = "entry")]
        public uint Entry { get; set; }

        public uint DifficultyEntry1 => 0;
        
        public uint DifficultyEntry2 => 0;
        
        public uint DifficultyEntry3 => 0;

        [Column(Name = "scale")]
        public float Scale { get; set; }

        // change to creature_template_gossip 1 to many table
        // [Column(Name = "gossip_menu_id")] 
        public uint GossipMenuId { get; set; }

        // master no longer have min/max level
        //[Column(Name = "minlevel")]
        public short MinLevel { get; set; }
        
        // master no longer have min/max level
        //[Column(Name = "maxlevel")]
        public short MaxLevel { get; set; }

        [Column(Name = "name")] 
        public string? _Name { get; set; } = "";

        public string Name => _Name ?? "";
        
        [Column(Name = "subname")]
        public string? SubName { get; set; } = "";
        
        [Column(Name = "AIName")]
        public string AIName { get; set; } = "";

        [Column(Name = "ScriptName")]
        public string ScriptName { get; set; } = "";
        
        [Column(Name = "unit_flags")]
        public GameDefines.UnitFlags UnitFlags { get; set; }

        [Column(Name = "unit_flags2")]
        public GameDefines.UnitFlags2 UnitFlags2 { get; set; }
        
        [Column(Name = "npcflag")]
        public GameDefines.NpcFlags NpcFlags { get; set; }
        
        [Column(Name = "speed_walk")]
        public float SpeedWalk { get; set; }
        
        [Column(Name = "speed_run")]
        public float SpeedRun { get; set; }

        [Column(Name = "faction")]
        public uint FactionTemplate { get; set; }
        
        public uint? EquipmentTemplateId => null;
        
        [Column(Name = "IconName")] 
        public string? IconName { get; set; }

        [Column(Name = "RequiredExpansion")] 
        public short RequiredExpansion { get; set; }
        
        [Column(Name = "Classification")] 
        public byte Rank { get; set; }
        
        [Column(Name = "unit_class")] 
        public byte UnitClass { get; set; }
        
        [Column(Name = "family")] 
        public int Family { get; set; }
        
        [Column(Name = "type")] 
        public byte Type { get; set; }
        
        public uint TypeFlags { get; set; }
        
        [Column(Name = "VehicleId")] 
        public uint VehicleId { get; set; }
        
        public float HealthMod { get; set; }
        
        public float ManaMod { get; set; }
        
        [Column(Name = "RacialLeader")] 
        public bool RacialLeader { get; set; }
        
        [Column(Name = "movementId")] 
        public uint MovementId { get; set; }
        
        [Column(Name = "KillCredit1")] 
        public uint KillCredit1 { get; set; }
        
        [Column(Name = "KillCredit2")] 
        public uint KillCredit2 { get; set; }

        [Column(Name = "flags_extra")]
        public uint FlagsExtra { get; set; }

        public GameDefines.InhabitType InhabitType => 0;

        private IReadOnlyList<CreatureTemplateModel>? models;
        
        public int ModelsCount => models?.Count ?? 0;
        
        public uint GetModel(int index)
        {
            if (models == null)
                return 0;
            return models.FirstOrDefault(x => x.Index == index)?.CreatureDisplayId ?? 0;
        }

        public ICreatureTemplate? WithModels(IReadOnlyList<CreatureTemplateModel> models)
        {
            this.models = models;
            return this;
        }

        public uint LootId => 0;

        public uint PickpocketLootId => 0;

        public uint SkinningLootId => 0;
    
        public int LootCount => 1;
        
        public uint GetLootId(int index) => LootId;
    }

    [Table(Name = "creature_template_model")]
    public class CreatureTemplateModel
    {
        [PrimaryKey]
        [Identity]
        [Column(Name = "CreatureID")]
        public uint CreatureId { get; set; }

        [PrimaryKey]
        [Identity]
        [Column(Name = "Idx")]
        public uint Index { get; set; }
        
        [Column(Name = "CreatureDisplayID")]
        public uint CreatureDisplayId { get; set; }
        
        [Column(Name = "DisplayScale")]
        public float DisplayScale { get; set; }
        
        [Column(Name = "Probability")]
        public float Probability { get; set; }
        
        [Column(Name = "VerifiedBuild")]
        public int VerifiedBuild { get; set; }
    }
}