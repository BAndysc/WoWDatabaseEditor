using LinqToDB.Mapping;
using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.CMMySqlDatabase.Models.Wrath
{
    [Table(Name = "creature")]
    public class CreatureWoTLK : ICreature
    {
        [PrimaryKey]
        [Identity]
        [Column(Name = "guid")]
        public uint Guid { get; set; }

        [Column(Name = "id")]
        public uint Entry { get; set; }

        [Column(Name = "map")]
        public int Map { get; set; }

        [Column(Name = "phaseMask")]
        public uint? PhaseMask { get; set; }

        public SmallReadOnlyList<int>? PhaseId => null;
        
        public int? PhaseGroup => null;

        public int EquipmentId => 0;

        public uint Model => 0;

        [Column(Name = "MovementType")]
        public MovementType MovementType { get; set; }
        
        [Column(Name = "spawndist")]
        public float WanderDistance { get; set; }

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