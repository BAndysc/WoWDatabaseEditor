using System.Threading.Tasks;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [UniqueProvider]
    public interface IDbEditorTableDataProvider
    {
        Task<IDbTableData?> Load(string tableName, uint key);
    }
}