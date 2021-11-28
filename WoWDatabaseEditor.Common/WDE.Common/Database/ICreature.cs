namespace WDE.Common.Database
{
    public interface ICreature
    {
        uint Guid { get; }
        uint Entry { get; }
        
        uint Map { get; }
        public uint PhaseMask { get; }
        float X { get; }
        float Y { get; }
        float Z { get; }
        float O { get; }
        // float MovementType { get; }

        uint SpawnKey => 0;
    }
}