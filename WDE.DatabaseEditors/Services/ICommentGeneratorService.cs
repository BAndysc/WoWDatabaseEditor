using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

[UniqueProvider]
public interface ICommentGeneratorService
{
    string GenerateFinalComment(DatabaseEntity entity, DatabaseTableDefinitionJson tableDefinition, ColumnFullName columnName);
    string GenerateAutoCommentOnly(DatabaseEntity entity, DatabaseTableDefinitionJson tableDefinition, ColumnFullName columnName);
}