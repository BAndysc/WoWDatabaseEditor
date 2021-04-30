using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.QueryGenerators
{
    public interface IQueryGenerator
    {
        public string GenerateQuery(ICollection<uint> keys, IDatabaseTableData tableData);
        public string GenerateUpdateFieldQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity, IDatabaseField field);
        public string GenerateDeleteQuery(DatabaseTableDefinitionJson table, DatabaseEntity entity);
    }
}