using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.SqlImport;

internal interface IDatabaseImportService
{
    Task ShowImportDatabaseWindowAsync(DatabaseCredentials credentials);
}