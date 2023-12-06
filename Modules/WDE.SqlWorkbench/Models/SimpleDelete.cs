namespace WDE.SqlWorkbench.Models;

internal readonly struct SimpleDelete
{
    public readonly SimpleFrom From;
    public readonly string? Where;

    public SimpleDelete(SimpleFrom from, string? where)
    {
        From = from;
        Where = where;
    }
    
    public override string ToString()
    {
        if (string.IsNullOrEmpty(Where))
            return $"DELETE FROM {From}";
        else
            return $"DELETE FROM {From} WHERE {Where}";
    }
}