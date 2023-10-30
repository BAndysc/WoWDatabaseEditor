using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "quest_template_addon")]
    public abstract class MySqlBaseQuestTemplateAddon
    {
        [PrimaryKey]
        [Column(Name = "ID")]
        public uint Entry { get; set; }

        [Column(Name = "PrevQuestId")]
        public int PrevQuestId { get; set; }

        [Column(Name = "NextQuestId")]
        public int NextQuestId { get; set; }

        [Column(Name = "ExclusiveGroup")]
        public int ExclusiveGroup { get; set; }
        
        [Column(Name = "AllowableClasses")]
        public uint AllowableClasses { get; set; }
        
        [Column(Name = "RewardMailTemplateId")]
        public uint RewardMailTemplateId { get; set; }

        public abstract int BreadcrumbForQuest { get; set; } 
    }
    
    [Table(Name = "quest_template_addon")]
    public class MySqlAzerothQuestTemplateAddon : MySqlBaseQuestTemplateAddon
    {
        public override int BreadcrumbForQuest { get; set; }
    }

    [Table(Name = "quest_template_addon")]
    public class MySqlWrathQuestTemplateAddon : MySqlBaseQuestTemplateAddon
    {
        [Column(Name = "BreadcrumbForQuestId")]
        public override int BreadcrumbForQuest { get; set; }
    }

    [Table(Name = "quest_template_addon")]
    public class MySqlCataQuestTemplateAddon : MySqlWrathQuestTemplateAddon
    {
        [Column(Name="AllowableRaces")]
        public CharacterRaces AllowableRaces { get; set; }
    }

    [Table(Name = "quest_template_addon")]
    public class MySqlQuestTemplateAddonWithScriptName : MySqlWrathQuestTemplateAddon
    {
        [Column(Name = "ScriptName")] 
        public string ScriptName { get; set; } = "";
    }
}