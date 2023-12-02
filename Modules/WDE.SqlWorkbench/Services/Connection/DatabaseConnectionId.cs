namespace WDE.SqlWorkbench.Services.Connection;

internal readonly struct DatabaseConnectionId
{
    private readonly int id;
    
    public DatabaseConnectionId(int id)
    {
        this.id = id + 1;
    }

    public int Id => id - 1;

    public static readonly DatabaseConnectionId NullConnection = default;
}