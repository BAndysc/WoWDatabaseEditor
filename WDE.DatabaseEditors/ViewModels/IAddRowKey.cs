using WDE.Common.Services;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels
{
    public interface IAddRowKey
    {
        DatabaseEntity AddRow(DatabaseKey key);
    }
}