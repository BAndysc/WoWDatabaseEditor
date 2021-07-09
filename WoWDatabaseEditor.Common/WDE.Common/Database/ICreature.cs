namespace WDE.Common.Database
{
    public interface ICreature
    {
        uint Guid { get; }
        uint Entry { get; }
        
        uint SpawnKey => 0;
    }
}