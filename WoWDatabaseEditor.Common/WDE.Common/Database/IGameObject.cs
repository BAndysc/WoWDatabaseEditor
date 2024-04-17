using WDE.Common.Utils;

namespace WDE.Common.Database
{
    public interface IGameObject
    {
        uint Guid { get; }
        uint Entry { get; }

        public int Map { get; }
        public uint? PhaseMask { get; }
        SmallReadOnlyList<int>? PhaseId { get; }
        int? PhaseGroup { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float Orientation { get; }
        public float Rotation0 { get; }
        public float Rotation1 { get; }
        public float Rotation2 { get; }
        public float Rotation3 { get; }
        public float ParentRotation0 { get; }
        public float ParentRotation1 { get; }
        public float ParentRotation2 { get; }
        public float ParentRotation3 { get; }

        uint SpawnKey => 0;
    }

    public static class GameObjectExtensions
    {
        public static SpawnKey Key(this IGameObject that)
        {
            return new SpawnKey(that.Entry, that.Guid);
        }
    }
}