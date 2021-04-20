using System.Threading.Tasks;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Loaders
{
    [UniqueProvider]
    public interface IDatabaseTableDataProvider
    {
        Task<IDatabaseTableData?> Load(string tableName, params uint[] keys);
    }
}