using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Factories
{
    public interface IDatabaseFieldFactory
    {
        IDatabaseField CreateField(string columnName, IValueHolder valueHolder);
    }
}