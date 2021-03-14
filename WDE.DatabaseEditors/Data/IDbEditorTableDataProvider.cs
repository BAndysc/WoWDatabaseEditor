using System.Threading.Tasks;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [UniqueProvider]
    public interface IDbEditorTableDataProvider
    {
        Task<IDbTableData?> LoadCreatureTemplateDataEntry(uint creatureEntry);
        Task<IDbTableData?> LoadGameobjectTemplateDataEntry(uint goEntry);
        Task<IDbTableData?> LoadCreatureLootTemplateData(uint entry);
    }
}