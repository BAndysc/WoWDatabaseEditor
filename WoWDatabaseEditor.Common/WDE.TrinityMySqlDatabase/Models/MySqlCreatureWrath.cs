using LinqToDB.Mapping;
using WDE.Common.Database;
using WDE.Common.Utils;

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
        public int Map { get; set; }

        [Column(Name = "phaseMask")]
        public uint? PhaseMask { get; set; }

        public SmallReadOnlyList<int>? PhaseId => null;
        
        public int? PhaseGroup => null;

        [Column(Name = "modelid")]
        public uint Model { get; set; }
        
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

        [Column(Name = "MovementType")]
        public MovementType MovementType { get; set; }
        
        [Column(Name = "wander_distance")]
        public float WanderDistance { get; set; }
        
        public uint SpawnKey => 0;
    }
    
    [Table(Name = "creature")]
    public class MySqlCreatureAzeroth : ICreature
    {
        [PrimaryKey]
        [Identity]
        [Column(Name = "guid")]
        public uint Guid { get; set; }

        [Column(Name = "id1")]
        public uint Entry { get; set; }

        [Column(Name = "map")]
        public int Map { get; set; }

        [Column(Name = "phaseMask")]
        public uint? PhaseMask { get; set; }

        public SmallReadOnlyList<int>? PhaseId => null;
        
        public int? PhaseGroup => null;

        public uint Model { get; set; }
        
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

        [Column(Name = "MovementType")]
        public MovementType MovementType { get; set; }
        
        [Column(Name = "wander_distance")]
        public float WanderDistance { get; set; }

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
        public int Map { get; set; }

        public uint? PhaseMask => null;
        
        [Column(Name = "PhaseId")]
        public int? phaseId { get; set; }

        public SmallReadOnlyList<int>? PhaseId => phaseId is null or 0 ? null : new SmallReadOnlyList<int>(phaseId.Value);
        
        [Column(Name = "PhaseGroup")]
        public int? PhaseGroup { get; set; }

        [Column(Name = "modelid")]
        public uint Model { get; set; }
        
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

        [Column(Name = "MovementType")]
        public MovementType MovementType { get; set; }
        
        [Column(Name = "wander_distance")]
        public float WanderDistance { get; set; }

        public uint SpawnKey => 0;
    }
    
    [Table(Name = "creature")]
    public class MySqlCreatureMaster : ICreature
    {
        [PrimaryKey]
        [Identity]
        [Column(Name = "guid")]
        public uint Guid { get; set; }

        [Column(Name = "id")]
        public uint Entry { get; set; }

        [Column(Name = "map")]
        public int Map { get; set; }

        public uint? PhaseMask => null;

        [Column(Name = "PhaseId")]
        public int? phaseId { get; set; }

        public SmallReadOnlyList<int>? PhaseId => phaseId is null or 0 ? null : new SmallReadOnlyList<int>(phaseId.Value);
        
        [Column(Name = "PhaseGroup")]
        public int? PhaseGroup { get; set; }

        [Column(Name = "modelid")]
        public uint Model { get; set; }
        
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

        [Column(Name = "MovementType")]
        public MovementType MovementType { get; set; }
        
        [Column(Name = "wander_distance")]
        public float WanderDistance { get; set; }

        public uint SpawnKey => 0;
    }
}