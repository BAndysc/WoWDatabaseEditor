using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    [Table("spell_template")]
    public class SpellDbcWoTLK : IDatabaseSpellDbc
    {
        [PrimaryKey]
        [Column(Name = "Id")]
        public uint Id { get; set; }

        [Column(Name = "SpellName")]
        public string? Name { get; set; }
    }
}