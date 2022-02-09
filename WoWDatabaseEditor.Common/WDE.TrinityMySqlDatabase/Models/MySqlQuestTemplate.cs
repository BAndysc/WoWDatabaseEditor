using System;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "quest_template")]
    public class MySqlQuestTemplate : IQuestTemplate
    {
        private MySqlQuestTemplateAddon? addon;

        [PrimaryKey]
        [Column(Name = "ID")]
        public uint Entry { get; set; }

        [Column(Name = "LogTitle")]
        public string Name { get; set; } = "";

        public CharacterClasses AllowableClasses => addon == null ? CharacterClasses.None : (CharacterClasses)addon.AllowableClasses;

        [Column(Name="AllowableRaces")]
        public CharacterRaces AllowableRaces { get; set; }
        
        public int PrevQuestId => addon == null ? 0 : addon.PrevQuestId;

        public int NextQuestId => addon == null ? 0 : addon.NextQuestId;

        public int ExclusiveGroup => addon == null ? 0 : addon.ExclusiveGroup;
        
        public int BreadcrumbForQuestId => addon?.BreadcrumbForQuest ?? 0;

        [Column(Name = "RewardNextQuest")]
        public uint NextQuestInChain { get; set; }

        public MySqlQuestTemplate SetAddon(MySqlQuestTemplateAddon? addon)
        {
            this.addon = addon;
            return this;
        }
    }
    
    [Table(Name = "quest_template")]
    public class MySqlMasterQuestTemplate : IQuestTemplate
    {
        private MySqlQuestTemplateAddon? addon;

        [PrimaryKey]
        [Column(Name = "ID")]
        public uint Entry { get; set; }

        [Column(Name = "LogTitle")]
        public string Name { get; set; } = "";

        public CharacterClasses AllowableClasses => addon == null ? CharacterClasses.None : (CharacterClasses)addon.AllowableClasses;

        public int BreadcrumbForQuestId => addon?.BreadcrumbForQuest ?? 0;
        
        [Column(Name="AllowableRaces")]
        private ulong allowableRaces { get; set; }

        public CharacterRaces AllowableRaces
        {
            get
            {
                if (allowableRaces == ulong.MaxValue)
                    return CharacterRaces.All;
                CharacterRaces races = CharacterRaces.None;
                foreach (var race in Enum.GetValues<CharacterRaces>())
                {
                    if (((ulong)race & allowableRaces) == (ulong)race)
                        races |= race;
                }

                return races;
            }
        }
        
        public int PrevQuestId => addon == null ? 0 : addon.PrevQuestId;

        public int NextQuestId => addon == null ? 0 : addon.NextQuestId;

        public int ExclusiveGroup => addon == null ? 0 : addon.ExclusiveGroup;

        [Column(Name = "RewardNextQuest")]
        public uint NextQuestInChain { get; set; }

        public MySqlMasterQuestTemplate SetAddon(MySqlQuestTemplateAddon? addon)
        {
            this.addon = addon;
            return this;
        }
    }
}