using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "spell_dbc")]
    public class TrinityMySqlSpellDbc : IDatabaseSpellDbc
    {
        [PrimaryKey]
        [Column(Name = "Id")]
        public uint Id { get; set; }

        [Column(Name = "SpellName")]
        public string? Name { get; set; }
    }
}