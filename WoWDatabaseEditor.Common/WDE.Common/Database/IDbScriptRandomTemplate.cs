namespace WDE.Common.Database;

public interface IDbScriptRandomTemplate
{
    public uint Id { get; }
    public uint Type { get; }
    public int Value { get; }
    public int Chance { get; }
    public string? Comment { get; }
}