using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.Services;

[UniqueProvider]
public interface IDefinitionGeneratorService
{
    Task<DatabaseTableDefinitionJson> GenerateDefinition(DatabaseTable tableName);
}