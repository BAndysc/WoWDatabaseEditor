using System;
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

        [Column(Name = "AIName")]
        public string AIName { get; set; } = "";

        [Column(Name = "ScriptName")]
        public string ScriptName { get; set; } = "";
        
        [Column(Name = "unit_flags")]
        public GameDefines.UnitFlags UnitFlags { get; set; }
        
        [Column(Name = "npcflag")]
        public GameDefines.NpcFlags NpcFlags { get; set; }
        
        public uint? EquipmentTemplateId => null;
        
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
    }
    
    [Table(Name = "creature_template")]
    public class MySqlCreatureTemplateMaster : ICreatureTemplate
    {
        [PrimaryKey]
        [Identity]
        [Column(Name = "entry")]
        public uint Entry { get; set; }
        
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

        [Column(Name = "AIName")]
        public string AIName { get; set; } = "";

        [Column(Name = "ScriptName")]
        public string ScriptName { get; set; } = "";
        
        [Column(Name = "unit_flags")]
        public GameDefines.UnitFlags UnitFlags { get; set; }
        
        [Column(Name = "npcflag")]
        public GameDefines.NpcFlags NpcFlags { get; set; }

        public uint? EquipmentTemplateId => null;

        public int ModelsCount => 0;
        public uint GetModel(int index)
        {
            throw new Exception("Model out of range");
        }
    }
}