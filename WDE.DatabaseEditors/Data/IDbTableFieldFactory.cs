using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    public interface IDbTableFieldFactory
    {
        IDbTableField CreateField(in DbEditorTableGroupFieldJson definition, object dbValue);
    }
}