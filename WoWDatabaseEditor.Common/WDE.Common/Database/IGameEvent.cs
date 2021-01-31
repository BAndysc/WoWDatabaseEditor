namespace WDE.Common.Database
{
    public interface IGameEvent
    {
        ushort Entry { get; }
        string Description { get; }
    }
}