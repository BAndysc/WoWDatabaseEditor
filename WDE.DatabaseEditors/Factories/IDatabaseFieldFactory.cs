using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Factories
{
    public interface IDatabaseFieldFactory
    {
        IDatabaseField CreateField(ColumnFullName columnName, IValueHolder valueHolder);
        IDatabaseField CreateField(DatabaseColumnJson column, object? value);
    }
}