using System.Collections.Generic;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.QueryGenerators;

[NonUniqueProvider]
public interface ICustomQueryGenerator
{
    string TableName { get; }
    IQuery Generate(IReadOnlyList<DatabaseKey> keys, IReadOnlyList<DatabaseKey>? deletedKeys, IDatabaseTableData data);
}