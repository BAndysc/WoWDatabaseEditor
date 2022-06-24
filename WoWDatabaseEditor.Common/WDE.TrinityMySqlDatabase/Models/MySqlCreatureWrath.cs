using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "creature")]
    public class MySqlCreatureWrath : ICreature
    {
        [PrimaryKey]
        [Identity]
        [Column(Name = "guid")]
        public uint Guid { get; set; }

        [Column(Name = "id")]
        public uint Entry { get; set; }

        [Column(Name = "map")]
        public uint Map { get; set; }

        [Column(Name = "phaseMask")]
        public uint? PhaseMask { get; set; }

        public int? PhaseId => null;
        
        public int? PhaseGroup => null;

        [Column(Name = "equipment_id")]
        public int EquipmentId { get; set; }
        
        [Column(Name = "position_x")]
        public float X { get; set; }

        [Column(Name = "position_y")]
        public float Y { get; set; }

        [Column(Name = "position_z")]
        public float Z { get; set; }

        [Column(Name = "orientation")]
        public float O { get; set; }

        public uint SpawnKey => 0;
    }
    
    [Table(Name = "creature")]
    public class MySqlCreatureCata : ICreature
    {
        [PrimaryKey]
        [Identity]
        [Column(Name = "guid")]
        public uint Guid { get; set; }

        [Column(Name = "id")]
        public uint Entry { get; set; }

        [Column(Name = "map")]
        public uint Map { get; set; }

        public uint? PhaseMask => null;

        [Column(Name = "PhaseId")]
        public int? PhaseId { get; set; }
        
        [Column(Name = "PhaseGroup")]
        public int? PhaseGroup { get; set; }

        [Column(Name = "equipment_id")]
        public int EquipmentId { get; set; }
        
        [Column(Name = "position_x")]
        public float X { get; set; }

        [Column(Name = "position_y")]
        public float Y { get; set; }

        [Column(Name = "position_z")]
        public float Z { get; set; }

        [Column(Name = "orientation")]
        public float O { get; set; }

        public uint SpawnKey => 0;
    }
}