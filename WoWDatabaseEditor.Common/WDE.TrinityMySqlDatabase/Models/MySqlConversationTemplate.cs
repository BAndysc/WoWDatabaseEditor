using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "conversation_template")]
    public class MySqlConversationTemplate : IConversationTemplate
    {
        [Column(Name = "Id")]
        [PrimaryKey]
        public uint Id { get; set; }
        
        [Column(Name = "FirstLineId")]
        public uint FirstLineId { get; set; }

        [Column(Name = "ScriptName")]
        public string ScriptName { get; set; } = "";
    }
}