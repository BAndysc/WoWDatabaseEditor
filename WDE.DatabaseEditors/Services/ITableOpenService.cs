using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

[UniqueProvider]
public interface ITableOpenService
{
    Task<ISolutionItem?> TryCreate(DatabaseTableDefinitionJson definition);
    Task<ISolutionItem?> Create(DatabaseTableDefinitionJson definition, DatabaseKey key);
}