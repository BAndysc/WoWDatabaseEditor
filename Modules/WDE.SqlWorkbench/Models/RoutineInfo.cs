using MySqlConnector;

namespace WDE.SqlWorkbench.Models;

internal readonly struct RoutineInfo
{
    public readonly string Schema;
    public readonly string Name;
    public readonly RoutineType Type;
    public readonly string? FunctionReturnType;
    public readonly string? FunctionFullReturnType;
    public readonly string? Body;
    public readonly bool IsDeterministic;
    public readonly SqlDataAccessType DataAccessType;
    public readonly SecurityType SecurityType;
    public readonly MySqlDateTime Created;
    public readonly MySqlDateTime LastAltered;
    public readonly string? Comment;
    public readonly string? Definer;

    public RoutineInfo(string schema, string name, RoutineType type, string? functionReturnType, string? functionFullReturnType, string? body, bool isDeterministic, SqlDataAccessType dataAccessType, SecurityType securityType, MySqlDateTime created, MySqlDateTime lastAltered, string? comment, string? definer)
    {
        Schema = schema;
        Name = name;
        Type = type;
        FunctionReturnType = functionReturnType;
        FunctionFullReturnType = functionFullReturnType;
        Body = body;
        IsDeterministic = isDeterministic;
        DataAccessType = dataAccessType;
        SecurityType = securityType;
        Created = created;
        LastAltered = lastAltered;
        Comment = comment;
        Definer = definer;
    }
}