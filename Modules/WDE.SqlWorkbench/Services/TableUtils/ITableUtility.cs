using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Services.TableUtils;

[UniqueProvider]
internal interface ITableUtility
{
    void OpenSelectRows(IConnection connection, string schema, string table);
    void InspectTable(IConnection connection, string schema, string table);
    void InspectDatabase(IConnection connection, string schema);
    void AlterTable(IConnection connection, string schema, string? table);
}