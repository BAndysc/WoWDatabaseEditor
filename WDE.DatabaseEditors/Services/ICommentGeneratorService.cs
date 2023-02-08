using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

[UniqueProvider]
public interface ICommentGeneratorService
{
    string GenerateFinalComment(DatabaseEntity entity, DatabaseTableDefinitionJson tableDefinition, string columnName);
    string GenerateAutoCommentOnly(DatabaseEntity entity, DatabaseTableDefinitionJson tableDefinition, string columnName);
}