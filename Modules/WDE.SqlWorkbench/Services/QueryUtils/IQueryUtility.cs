using System.Text;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.QueryUtils;

internal interface IQueryUtility
{
    bool IsSimpleSelect(string query, out SimpleSelect select);
    bool IsUseDatabase(string query, out string databaseName);
}