using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonQuestTemplate : IQuestTemplate
    {
        public uint Entry { get; set; }

        public string Name { get; set; } = "";

        public int MinLevel { get; set; }
        
        public int QuestSortId { get; set; }
        
        public CharacterClasses AllowableClasses { get; set; }

        public CharacterRaces AllowableRaces { get; set; }
        
        public int PrevQuestId { get; set; }

        public int NextQuestId { get; set; }

        public int ExclusiveGroup  { get; set; }
        
        public int BreadcrumbForQuestId  { get; set; }

        public uint NextQuestInChain { get; set; }

        public uint RewardMailTemplateId { get; set; }

        public uint QuestRewardId { get; set; }
    }
    

    public class JsonCataQuestTemplate : IQuestTemplate
    {
        private JsonCataQuestTemplateAddon? addon;

        
        public uint Entry { get; set; }

        
        public string Name { get; set; } = "";

        
        public int MinLevel { get; set; }

        
        public int QuestSortId { get; set; }
        
        public CharacterClasses AllowableClasses => addon == null ? CharacterClasses.None : (CharacterClasses)addon.AllowableClasses;

        public CharacterRaces AllowableRaces => addon?.AllowableRaces ?? CharacterRaces.All;
        
        public int PrevQuestId => addon == null ? 0 : addon.PrevQuestId;

        public int NextQuestId => addon == null ? 0 : addon.NextQuestId;

        public int ExclusiveGroup => addon == null ? 0 : addon.ExclusiveGroup;
        
        public int BreadcrumbForQuestId => addon?.BreadcrumbForQuest ?? 0;

        
        public uint NextQuestInChain { get; set; }

        public uint RewardMailTemplateId => addon?.RewardMailTemplateId ?? 0;

        public uint QuestRewardId => 0;
        
        public JsonCataQuestTemplate SetAddon(JsonCataQuestTemplateAddon? addon)
        {
            this.addon = addon;
            return this;
        }
    }
    

    public class JsonMasterQuestTemplate : IQuestTemplate
    {
        private JsonBaseQuestTemplateAddon? addon;

        
        public uint Entry { get; set; }

        
        public string Name { get; set; } = "";

        // master no longer has min level
        //
        public int MinLevel { get; set; }

        
        public int QuestSortId { get; set; }
        
        public CharacterClasses AllowableClasses => addon == null ? CharacterClasses.None : (CharacterClasses)addon.AllowableClasses;

        public int BreadcrumbForQuestId => addon?.BreadcrumbForQuest ?? 0;

        private ulong allowableRaces { get; set; }

        public uint RewardMailTemplateId => addon?.RewardMailTemplateId ?? 0;
        
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

        
        public uint NextQuestInChain { get; set; }

        public uint QuestRewardId => 0;
        
        public JsonMasterQuestTemplate SetAddon(JsonBaseQuestTemplateAddon? addon)
        {
            this.addon = addon;
            return this;
        }
    }
}