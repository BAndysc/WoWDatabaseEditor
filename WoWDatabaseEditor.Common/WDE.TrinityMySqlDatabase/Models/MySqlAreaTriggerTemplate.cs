using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "areatrigger_template")]
    public class MySqlAreaTriggerTemplate : IAreaTriggerTemplate
    {
        [PrimaryKey]
        [Column(Name = "Id")]
        public uint Id { get; set; }

        [PrimaryKey]
        [Column(Name = "IsCustom")]
        public bool IsServerSide { get; set; }

        public string? Name => null;
        public string? ScriptName => null;
        public string? AIName => null;
    }
}