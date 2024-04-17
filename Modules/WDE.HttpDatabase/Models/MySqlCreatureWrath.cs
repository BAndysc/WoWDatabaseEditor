using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.HttpDatabase.Models
{

    public class JsonCreatureWrath : ICreature
    {
        
        
        public uint Guid { get; set; }

        
        public uint Entry { get; set; }

        
        public int Map { get; set; }

        
        public uint? PhaseMask { get; set; }

        public SmallReadOnlyList<int>? PhaseId => null;
        
        public int? PhaseGroup => null;

        
        public uint Model { get; set; }
        
        
        public int EquipmentId { get; set; }
        
        
        public float X { get; set; }

        
        public float Y { get; set; }

        
        public float Z { get; set; }

        
        public float O { get; set; }

        
        public MovementType MovementType { get; set; }
        
        
        public float WanderDistance { get; set; }
        
        public uint SpawnKey => 0;
    }
    

    public class JsonCreatureAzeroth : ICreature
    {
        
        
        public uint Guid { get; set; }

        
        public uint Entry { get; set; }

        
        public int Map { get; set; }

        
        public uint? PhaseMask { get; set; }

        public SmallReadOnlyList<int>? PhaseId => null;
        
        public int? PhaseGroup => null;

        public uint Model { get; set; }
        
        
        public int EquipmentId { get; set; }
        
        
        public float X { get; set; }

        
        public float Y { get; set; }

        
        public float Z { get; set; }

        
        public float O { get; set; }

        
        public MovementType MovementType { get; set; }
        
        
        public float WanderDistance { get; set; }

        public uint SpawnKey => 0;
    }
    

    public class JsonCreatureCata : ICreature
    {
        
        
        public uint Guid { get; set; }

        
        public uint Entry { get; set; }

        
        public int Map { get; set; }

        public uint? PhaseMask => null;
        
        
        public int? phaseId { get; set; }

        public SmallReadOnlyList<int>? PhaseId => phaseId is null or 0 ? null : new SmallReadOnlyList<int>(phaseId.Value);
        
        
        public int? PhaseGroup { get; set; }

        
        public uint Model { get; set; }
        
        
        public int EquipmentId { get; set; }
        
        
        public float X { get; set; }

        
        public float Y { get; set; }

        
        public float Z { get; set; }

        
        public float O { get; set; }

        
        public MovementType MovementType { get; set; }
        
        
        public float WanderDistance { get; set; }

        public uint SpawnKey => 0;
    }
    

    public class JsonCreatureMaster : ICreature
    {
        
        
        public uint Guid { get; set; }

        
        public uint Entry { get; set; }

        
        public int Map { get; set; }

        public uint? PhaseMask => null;

        
        public int? phaseId { get; set; }

        public SmallReadOnlyList<int>? PhaseId => phaseId is null or 0 ? null : new SmallReadOnlyList<int>(phaseId.Value);
        
        
        public int? PhaseGroup { get; set; }

        
        public uint Model { get; set; }
        
        
        public int EquipmentId { get; set; }
        
        
        public float X { get; set; }

        
        public float Y { get; set; }

        
        public float Z { get; set; }

        
        public float O { get; set; }

        
        public MovementType MovementType { get; set; }
        
        
        public float WanderDistance { get; set; }

        public uint SpawnKey => 0;
    }
}