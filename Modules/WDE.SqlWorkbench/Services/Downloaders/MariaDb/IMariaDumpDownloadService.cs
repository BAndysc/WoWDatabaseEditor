using System.Threading.Tasks;

namespace WDE.SqlWorkbench.Services.Downloaders.MariaDb;

internal interface IMariaDownloadService
{
    Task<(string dump, string mariadb)?> AskToDownloadMariaAsync();
}