using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Factories
{
    public interface IDatabaseFieldFactory
    {
        IDatabaseField CreateField(in DbEditorTableGroupFieldJson definition, object dbValue,
            IDatabaseColumn? targetedColumn = null);
    }
}