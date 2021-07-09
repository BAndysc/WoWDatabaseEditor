namespace WDE.Common.Database
{
    public interface IGameObject
    {
        uint Guid { get; }
        uint Entry { get; }

        uint SpawnKey => 0;
    }
}