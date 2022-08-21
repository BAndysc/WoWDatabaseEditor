namespace WDE.Common.Database;

public interface ICreatureAiSummon
{
    public uint Id { get; }
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float O { get; }
    public uint SpawnTimeSeconds { get; }
    public string Comment { get; }
}