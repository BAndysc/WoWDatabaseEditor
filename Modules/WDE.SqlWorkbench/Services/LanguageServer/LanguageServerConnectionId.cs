namespace WDE.SqlWorkbench.Services.LanguageServer;

internal readonly struct LanguageServerConnectionId
{
    private readonly int id;
    
    public LanguageServerConnectionId(int id)
    {
        this.id = id + 1;
    }

    public int Id => id - 1;

    public static readonly LanguageServerConnectionId NullConnection = default;
}