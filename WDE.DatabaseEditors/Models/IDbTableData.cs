using System.Collections.Generic;
using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public interface IDbTableData
    {
        string TableName { get; }
        string DbTableName { get; }
        string TableIndexFieldName { get; }
        string TableIndexValue { get; }
        string TableDescription { get; }
    }
}