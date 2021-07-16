using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "creature")]
    public class MySqlCreature : ICreature
    {
        [PrimaryKey]
        [Identity]
        [Column(Name = "guid")]
        public uint Guid { get; set; }

        [Column(Name = "id")]
        public uint Entry { get; set; }

        public uint Map { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float O { get; }

        public uint SpawnKey => 0;
    }
}