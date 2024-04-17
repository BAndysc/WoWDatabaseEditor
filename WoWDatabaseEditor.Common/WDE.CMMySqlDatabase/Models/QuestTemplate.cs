using System;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    [Table(Name = "quest_template")]
    public class QuestTemplateWoTLK : IQuestTemplate
    {
        //private MySqlQuestTemplateAddon? addon;

        [Column("entry"                 , IsPrimaryKey = true)] public uint    Entry                  { get; set; } // mediumint(8) unsigned

//         [Column(Name = "LogTitle")]
//         public string Name { get; set; } = "";
        [Column("Title"                                      )] public string  Name                   { get; set; } = ""; // text

        [Column(Name = "MinLevel")]
        public int MinLevel { get; set; }

        [Column(Name = "ZoneOrSort")]
        public int QuestSortId { get; set; }
        
//         [Column(Name="AllowableRaces")]
//         public CharacterRaces AllowableRaces { get; set; }
        [Column("RequiredRaces"                             )] public CharacterRaces AllowableRaces   { get; set; } // smallint(5) unsigned

        [Column("PrevQuestId"                                )] public int     PrevQuestId            { get; set; } // mediumint(9)

        [Column("NextQuestId"                                )] public int     NextQuestId            { get; set; } // mediumint(9)

        [Column("ExclusiveGroup"                             )] public int     ExclusiveGroup         { get; set; } // mediumint(9)
        
        [Column("BreadcrumbForQuestId"                       )] public int     BreadcrumbForQuestId   { get; set; } // mediumint(9) unsigned

        [Column("RequiredClasses"                            )] public CharacterClasses AllowableClasses { get; set; } // smallint(5) unsigned

        //         [Column(Name = "RewardNextQuest")]
        //         public uint NextQuestInChain { get; set; }

        [Column(Name = "RewMailTemplateId")]
        public uint RewardMailTemplateId { get; set; }

        public uint QuestRewardId => 0;

        // TODO: remove this hack by setting NextQuestInChain as int instead uint (this will ignore next quest id < 0)
        [Column("NextQuestInChain"                                )] public int    NextQuestInChainU            { get; set; } // mediumint(9)
        public uint NextQuestInChain => NextQuestInChainU > 0 ? (uint)NextQuestInChainU : 0;
// 
//         public QuestTemplateWoTLK SetAddon(MySqlQuestTemplateAddon? addon)
//         {
//             this.addon = addon;
//             return this;
//         }
    }
}