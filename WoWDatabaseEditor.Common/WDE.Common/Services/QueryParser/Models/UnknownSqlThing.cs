namespace WDE.Common.Services.QueryParser.Models;

public class UnknownSqlThing
{
    public UnknownSqlThing(string raw)
    {
        Raw = raw;
    }

    public string Raw { get; init; }
}