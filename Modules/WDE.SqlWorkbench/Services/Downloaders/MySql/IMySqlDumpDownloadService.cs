using System.Threading.Tasks;

namespace WDE.SqlWorkbench.Services.Downloaders.MySql;

internal interface IMySqlDownloadService
{
    Task<(string dump, string mysql)?> AskToDownloadMySqlAsync();
}