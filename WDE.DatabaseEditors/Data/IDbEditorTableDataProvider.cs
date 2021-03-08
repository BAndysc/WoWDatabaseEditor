using System.Threading.Tasks;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [UniqueProvider]
    public interface IDbEditorTableDataProvider
    {
        Task<IDbTableData> LoadCreatureTamplateDataEntry(uint creatureEntry);
        Task<IDbTableData> LoadGameobjectTamplateDataEntry(uint goEntry);
    }
}