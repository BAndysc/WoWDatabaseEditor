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
        
        [Column(Name = "npcflag")]
        public GameDefines.NpcFlags NpcFlags { get; set; }
        
        [Column(Name = "faction")]
        public uint FactionTemplate { get; set; }
        
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
        
        [Column(Name = "subname")]
        public string? SubName { get; set; } = "";
        
        [Column(Name = "AIName")]
        public string AIName { get; set; } = "";

        [Column(Name = "ScriptName")]
        public string ScriptName { get; set; } = "";
        
        [Column(Name = "unit_flags")]
        public GameDefines.UnitFlags UnitFlags { get; set; }
        
        [Column(Name = "npcflag")]
        public GameDefines.NpcFlags NpcFlags { get; set; }

        [Column(Name = "faction")]
        public uint FactionTemplate { get; set; }
        
        public uint? EquipmentTemplateId => null;

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