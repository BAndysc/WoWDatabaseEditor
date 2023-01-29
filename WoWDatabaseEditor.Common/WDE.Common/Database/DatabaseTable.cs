namespace WDE.Common.Database;

public readonly record struct DatabaseTable(DataDatabaseType Database, string Table)
{
    public readonly DataDatabaseType Database = Database;
    public readonly string Table = Table;

    public static DatabaseTable WorldTable(string name)
    {
        return new DatabaseTable(DataDatabaseType.World, name);
    }

    public static DatabaseTable HotfixTable(string name)
    {
        return new DatabaseTable(DataDatabaseType.Hotfix, name);
    }

    public override string ToString()
    {
        return $"{Database}.{Table}";
    }
    
    
}