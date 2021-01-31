using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "game_event")]
    public class MySqlGameEvent : IGameEvent
    {
        [PrimaryKey]
        [Column(Name = "eventEntry")]
        public ushort Entry { get; set; }

        [Column(Name = "description")]
        public string? Description { get; set; }
    }
}