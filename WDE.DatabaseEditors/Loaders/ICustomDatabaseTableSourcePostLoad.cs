using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Loaders;

[NonUniqueProvider]
public interface ICustomDatabaseTableSourcePostLoad
{
    DatabaseTable Table { get; }

    Task PostProcess(IDatabaseTableData data, DatabaseTableDefinitionJson tableDefinition, DatabaseKey[]? keys);
}