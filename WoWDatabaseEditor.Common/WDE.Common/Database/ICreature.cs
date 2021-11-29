namespace WDE.Common.Database
{
    public interface ICreature
    {
        uint Guid { get; }
        uint Entry { get; }
        
        uint Map { get; }
        uint? PhaseMask { get; }
        float X { get; }
        float Y { get; }
        float Z { get; }
        float O { get; }
        
        uint SpawnKey => 0;
    }
}