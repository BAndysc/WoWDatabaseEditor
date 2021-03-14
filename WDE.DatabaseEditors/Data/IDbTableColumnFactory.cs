using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Data
{
    public interface IDbTableColumnFactory
    {
        IDbTableColumn CreateColumn(in DbEditorTableGroupFieldJson definition);
    }
}