namespace WDE.Common.Database
{
    public interface IGameObject
    {
        uint Guid { get; }
        uint Entry { get; }

        public uint Map { get; }
        public uint PhaseMask { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float Orientation { get; }
        public float Rotation0 { get; }
        public float Rotation1 { get; }
        public float Rotation2 { get; }
        public float Rotation3 { get; }

        uint SpawnKey => 0;
    }
}