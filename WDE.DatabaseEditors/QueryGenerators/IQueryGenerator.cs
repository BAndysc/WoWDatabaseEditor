using System.Collections.Generic;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.QueryGenerators
{
    public interface IQueryGenerator
    {
        public IQuery GenerateInsertQuery(IReadOnlyList<DatabaseKey> keys, IDatabaseTableData tableData);
        public IQuery GenerateQuery(IReadOnlyList<DatabaseKey> keys, IReadOnlyList<DatabaseKey>? deletedKeys, IDatabaseTableData tableData);
        public IQuery GenerateUpdateFieldQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity, IDatabaseField field);
        public IQuery GenerateDeleteQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity);
        public IQuery GenerateDeleteQuery(DatabaseTableDefinitionJson table, DatabaseKey key);
    }
}