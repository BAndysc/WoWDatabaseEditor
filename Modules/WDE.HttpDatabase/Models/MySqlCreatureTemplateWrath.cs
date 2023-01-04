using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonCreatureTemplateWrath : ICreatureTemplate
    {
        
        
        public uint Entry { get; set; }

        
        public uint DifficultyEntry1 { get; set; }

        
        public uint DifficultyEntry2 { get; set; }

        
        public uint DifficultyEntry3 { get; set; }

        
        public uint ModelId1 { get; set; }

        
        public uint ModelId2 { get; set; }

        
        public uint ModelId3 { get; set; }

        
        public uint ModelId4 { get; set; }

        
        public float Scale { get; set; }

         
        public uint GossipMenuId { get; set; }

        
        public short MinLevel { get; set; }
        
        
        public short MaxLevel { get; set; }

         
        public string Name { get; set; } = "";
        
        
        public string? SubName { get; set; } = "";
        
        
        public string AIName { get; set; } = "";

        
        public string ScriptName { get; set; } = "";
        
        
        public GameDefines.UnitFlags UnitFlags { get; set; }

        
        public GameDefines.UnitFlags2 UnitFlags2 { get; set; }

        
        public GameDefines.NpcFlags NpcFlags { get; set; }
        
        
        public float SpeedWalk { get; set; }
        
        
        public float SpeedRun { get; set; }

        
        public uint FactionTemplate { get; set; }
        
        public uint? EquipmentTemplateId => null;
        
         
        public string? IconName { get; set; }

         
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

        public uint LootId { get; set; }
    
        public uint PickpocketLootId { get; set; }
    
        public uint SkinningLootId { get; set; }
    
        public int LootCount => 1;
        
        public uint GetLootId(int index) => LootId;
    }
    

    public class JsonCreatureTemplateMaster : ICreatureTemplate
    {
        
        
        public uint Entry { get; set; }

        public uint DifficultyEntry1 => 0;
        
        public uint DifficultyEntry2 => 0;
        
        public uint DifficultyEntry3 => 0;

        
        public float Scale { get; set; }

        // change to creature_template_gossip 1 to many table
        //  
        public uint GossipMenuId { get; set; }

        // master no longer have min/max level
        //
        public short MinLevel { get; set; }
        
        // master no longer have min/max level
        //
        public short MaxLevel { get; set; }

         
        public string Name { get; set; } = "";
        
        
        public string? SubName { get; set; } = "";
        
        
        public string AIName { get; set; } = "";

        
        public string ScriptName { get; set; } = "";
        
        
        public GameDefines.UnitFlags UnitFlags { get; set; }

        
        public GameDefines.UnitFlags2 UnitFlags2 { get; set; }
        
        
        public GameDefines.NpcFlags NpcFlags { get; set; }
        
        
        public float SpeedWalk { get; set; }
        
        
        public float SpeedRun { get; set; }

        
        public uint FactionTemplate { get; set; }
        
        public uint? EquipmentTemplateId => null;
        
         
        public string? IconName { get; set; }

         
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


    public class CreatureTemplateModel
    {
        
        
        public uint CreatureId { get; set; }

        
        
        public uint Index { get; set; }
        
        
        public uint CreatureDisplayId { get; set; }
        
        
        public float DisplayScale { get; set; }
        
        
        public float Probability { get; set; }
        
        
        public int VerifiedBuild { get; set; }
    }
}