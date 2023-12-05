using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.Connection;

internal interface IMySqlConnector
{
    Task<IRawMySqlConnection> ConnectAsync(DatabaseCredentials credentials, QueryExecutionSafety safeMode);
}

// DO NOT CHANGE ENUM NAMES!!!
[JsonConverter(typeof(StringEnumConverter))]
internal enum QueryExecutionSafety
{
    ExecuteAll,
    AskUnlessSelect,
    AlwaysAsk
}