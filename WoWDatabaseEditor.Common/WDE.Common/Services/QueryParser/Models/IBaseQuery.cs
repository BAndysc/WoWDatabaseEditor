using WDE.Common.Database;

namespace WDE.Common.Services.QueryParser.Models;

public interface IBaseQuery
{
    DatabaseTable TableName { get; }
}