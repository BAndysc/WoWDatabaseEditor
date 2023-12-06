namespace WDE.SqlWorkbench.Models;

internal readonly struct Collation
{
    public readonly string Name;
    public readonly string Charset;
    public readonly long Id;
    public readonly bool IsDefault;
    public readonly bool IsCompiled;

    public Collation(string name, string charset, long id, bool isDefault, bool isCompiled)
    {
        Name = name;
        Charset = charset;
        Id = id;
        IsDefault = isDefault;
        IsCompiled = isCompiled;
    }
}