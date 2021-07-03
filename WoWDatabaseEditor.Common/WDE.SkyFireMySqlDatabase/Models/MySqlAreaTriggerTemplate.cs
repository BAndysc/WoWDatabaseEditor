using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.SkyFireMySqlDatabase.Models
{
    [Table(Name = "areatrigger_template")]
    public class MySqlAreaTriggerTemplate : IAreaTriggerTemplate
    {
        [PrimaryKey]
        [Column(Name = "Id")]
        public uint Id { get; set; }

        [PrimaryKey]
        [Column(Name = "IsServerSide")]
        public bool IsServerSide { get; set; }
        
        [Column(Name = "ScriptName")]
        public string? ScriptName { get; set; }
    }
}