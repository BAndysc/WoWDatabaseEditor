using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Factories
{
    public interface IDatabaseColumnFactory
    {
        IDatabaseColumn CreateColumn(in DbEditorTableGroupFieldJson definition, object? defaultValue);
    }
}