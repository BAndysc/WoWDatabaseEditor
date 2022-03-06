namespace WDE.Common.Database;

public interface ISpawnGroupTemplate
{
    uint Id { get; }
    string Name { get; }
    uint Flags { get; }
}