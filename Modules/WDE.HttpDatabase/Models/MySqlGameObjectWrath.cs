using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.HttpDatabase.Models
{

    public class JsonGameObjectWrath : IGameObject
    {
        public uint Guid { get; set; }

        
        public uint Entry { get; set; }
        
        public int Map { get; set; }

        public SmallReadOnlyList<int>? PhaseId => null;
        
        public int? PhaseGroup => null;
        
        
        public uint? PhaseMask { get; set; }

        
        public float X { get; set; }

        
        public float Y { get; set; }

        
        public float Z { get; set; }

        
        public float Orientation { get; set; }

        
        public float Rotation0 { get; set; }

        
        public float Rotation1 { get; set; }

        
        public float Rotation2 { get; set; }

        
        public float Rotation3 { get; set; }

        // in addon
        //
        public float ParentRotation0 { get; set; }

        //
        public float ParentRotation1 { get; set; }

        //
        public float ParentRotation2 { get; set; }

        //
        public float ParentRotation3 { get; set; }
    }
    
    

    public class JsonGameObjectCata : IGameObject
    {
        
        
        public uint Guid { get; set; }

        
        public uint Entry { get; set; }
        
        public int Map { get; set; }

        public uint? PhaseMask => null;

        
        public int? phaseId { get; set; }

        public SmallReadOnlyList<int>? PhaseId => phaseId is null or 0 ? null : new SmallReadOnlyList<int>(phaseId.Value);
        
        
        public int? PhaseGroup { get; set; }

        
        public float X { get; set; }

        
        public float Y { get; set; }

        
        public float Z { get; set; }

        
        public float Orientation { get; set; }

        
        public float Rotation0 { get; set; }

        
        public float Rotation1 { get; set; }

        
        public float Rotation2 { get; set; }

        
        public float Rotation3 { get; set; }

        // in addon
        //
        public float ParentRotation0 { get; set; }

        //
        public float ParentRotation1 { get; set; }

        //
        public float ParentRotation2 { get; set; }

        //
        public float ParentRotation3 { get; set; }
    }
}