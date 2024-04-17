
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public abstract class JsonBaseQuestTemplateAddon
    {
        public uint Entry { get; set; }

        public int PrevQuestId { get; set; }

        public int NextQuestId { get; set; }

        public int ExclusiveGroup { get; set; }
        
        public uint AllowableClasses { get; set; }
        
        public uint RewardMailTemplateId { get; set; }

        public abstract int BreadcrumbForQuest { get; set; } 
    }
    

    public class JsonAzerothQuestTemplateAddon : JsonBaseQuestTemplateAddon
    {
        public override int BreadcrumbForQuest { get; set; }
    }


    public class JsonWrathQuestTemplateAddon : JsonBaseQuestTemplateAddon
    {
        
        public override int BreadcrumbForQuest { get; set; }
    }


    public class JsonCataQuestTemplateAddon : JsonWrathQuestTemplateAddon
    {
        public CharacterRaces AllowableRaces { get; set; }
    }


    public class JsonQuestTemplateAddonWithScriptName : JsonWrathQuestTemplateAddon
    {
         
        public string ScriptName { get; set; } = "";
    }
}