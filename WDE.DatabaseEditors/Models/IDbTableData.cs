using System.Collections.Generic;
using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public interface IDbTableData
    {
        string TableName { get; }
        List<IDbTableColumnCategory> Categories { get; }
    }
}