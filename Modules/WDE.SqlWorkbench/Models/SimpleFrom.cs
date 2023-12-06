namespace WDE.SqlWorkbench.Models;

internal readonly struct SimpleFrom
{
    public readonly string? Schema;
    public readonly string Table;

    public SimpleFrom(string? schema, string table)
    {
        Schema = schema;
        Table = table;
    }

    public override string ToString()
    {
        if (Schema == null)
            return $"`{Table}`";
        else
            return $"`{Schema}`.`{Table}`";
    }
}