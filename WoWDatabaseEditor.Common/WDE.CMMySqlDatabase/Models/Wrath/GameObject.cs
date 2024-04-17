using LinqToDB.Mapping;
using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.CMMySqlDatabase.Models.Wrath
{
    [Table(Name = "gameobject")]
    public class GameObjectWoTLK : IGameObject
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

        [Column(Name = "position_x")]
        public float X { get; set; }

        [Column(Name = "position_y")]
        public float Y { get; set; }

        [Column(Name = "position_z")]
        public float Z { get; set; }

        [Column(Name = "orientation")]
        public float Orientation { get; set; }

        [Column(Name = "rotation0")]
        public float Rotation0 { get; set; }

        [Column(Name = "rotation1")]
        public float Rotation1 { get; set; }

        [Column(Name = "rotation2")]
        public float Rotation2 { get; set; }

        [Column(Name = "rotation3")]
        public float Rotation3 { get; set; }

        // in addon
        //[Column(Name = "parent_rotation0")]
        public float ParentRotation0 { get; set; }

        //[Column(Name = "parent_rotation1")]
        public float ParentRotation1 { get; set; }

        //[Column(Name = "parent_rotation2")]
        public float ParentRotation2 { get; set; }

        //[Column(Name = "parent_rotation3")]
        public float ParentRotation3 { get; set; }
    }
}