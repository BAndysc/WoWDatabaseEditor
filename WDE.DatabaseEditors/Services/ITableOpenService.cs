using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

[UniqueProvider]
public interface ITableOpenService
{
    Task<ISolutionItem?> TryCreate(DatabaseTableDefinitionJson definition);
    Task<IReadOnlyCollection<ISolutionItem>> TryCreateMultiple(DatabaseTableDefinitionJson definition);
    Task<ISolutionItem?> Create(DatabaseTableDefinitionJson definition, DatabaseKey key);
    
    Task<ISolutionItem?> TryCreate(DatabaseTable tableName);
    Task<IReadOnlyCollection<ISolutionItem>> TryCreateMultiple(DatabaseTable tableName);
}