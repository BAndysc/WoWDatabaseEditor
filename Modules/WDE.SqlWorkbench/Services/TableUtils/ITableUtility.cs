using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.TableUtils;

[UniqueProvider]
internal interface ITableUtility
{
    void OpenSelectRows(in DatabaseConnectionData connection, string schema, string table);
    void InspectTable(in DatabaseConnectionData connection, string schema, string table);
}