namespace WDE.SqlWorkbench.ViewModels;

internal readonly struct SchemaName
{
    public readonly string Name;

    public SchemaName(string name)
    {
        Name = name;
    }
}