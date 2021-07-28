using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.QueryGenerators
{
    public interface IQueryGenerator
    {
        public IQuery GenerateQuery(ICollection<uint> keys, IDatabaseTableData tableData);
        public IQuery GenerateUpdateFieldQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity, IDatabaseField field);
        public IQuery GenerateDeleteQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity);
    }
}