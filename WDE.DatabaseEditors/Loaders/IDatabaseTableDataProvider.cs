using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Loaders
{
    [UniqueProvider]
    public interface IDatabaseTableDataProvider
    {
        Task<IDatabaseTableData?> Load(DatabaseTable tableName, string? customWhere, long? offset, int? limit, DatabaseKey[]? keys);
        Task<long> GetCount(DatabaseTable tableName, string? customWhere, IEnumerable<DatabaseKey>? keys);
    }
}