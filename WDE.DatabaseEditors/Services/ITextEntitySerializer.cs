using System.Collections.Generic;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.Services;

[UniqueProvider]
public interface ITextEntitySerializer
{
    string Serialize(IEnumerable<DatabaseEntity> entities);
    IReadOnlyList<DatabaseEntity> Deserialize(DatabaseTableDefinitionJson definition, string json, DatabaseKey? forceKey);
}