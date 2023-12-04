using System.Threading.Tasks;

namespace WDE.SqlWorkbench.Services.Downloaders.MySql;

internal interface IMySqlDumpDownloadService
{
    Task<string?> AskToDownloadMySqlDumpAsync();
}