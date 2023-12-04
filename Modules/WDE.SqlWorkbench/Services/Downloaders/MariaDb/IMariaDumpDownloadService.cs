using System.Threading.Tasks;

namespace WDE.SqlWorkbench.Services.Downloaders.MariaDb;

internal interface IMariaDumpDownloadService
{
    Task<string?> AskToDownloadMariaDumpAsync();
}