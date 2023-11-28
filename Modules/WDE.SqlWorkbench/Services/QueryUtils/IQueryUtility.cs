using System.Text;

namespace WDE.SqlWorkbench.Services.QueryUtils;

internal interface IQueryUtility
{
    bool IsSimpleSelect(string query, out SimpleSelect select);
}