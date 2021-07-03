using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.SkyFireMySqlDatabase.Models
{
    [Table(Name = "quest_template")]
    public class MySqlQuestTemplate : IQuestTemplate
    {
        [PrimaryKey]
        [Column(Name = "ID")]
        public uint Entry { get; set; }

        [Column(Name = "Title")]
        public string Name { get; set; } = "";

        [Column(Name = "PrevQuestId")]
        public int PrevQuestId { get; set; }

        [Column(Name = "NextQuestId")]
        public int NextQuestId { get; set; }

        [Column(Name = "ExclusiveGroup")]
        public int ExclusiveGroup { get; set; }
        
        [Column(Name = "NextQuestIdChain")]
        public int NextQuestInChain { get; set; }
    }
}