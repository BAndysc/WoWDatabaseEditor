using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface ITableEditorPickerService
{
    Task<long?> PickByColumn(string table, uint key, string column, long? initialValue);
}