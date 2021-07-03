using LinqToDB.Mapping;

namespace WDE.SkyFireMySqlDatabase.Models
{
    [Table(Name = "areatrigger_scripts")]
    public class MySqlAreaTriggerScript
    {
        [PrimaryKey]
        [Column(Name = "entry")]
        public int Id { get; set; }

        [Column(Name = "ScriptName")]
        public string? ScriptName { get; set; }
    }
}