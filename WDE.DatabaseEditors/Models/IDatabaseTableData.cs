using System.Collections.Generic;
using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public interface IDatabaseTableData
    {
        string TableName { get; }
        string DbTableName { get; }
        string TableIndexFieldName { get; }
        string TableIndexValue { get; }
    }
}