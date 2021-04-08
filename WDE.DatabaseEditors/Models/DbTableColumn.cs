using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableColumn<T> : IDbTableColumn
    {
        public string ColumnName => FieldDataSource.Name;
        public string DbColumnName => FieldDataSource.DbColumnName;
        public ObservableCollection<IDbTableField> Fields { get; }
        public DbEditorTableGroupFieldJson FieldDataSource { get; }
        private object defaultValue;

        public DbTableColumn(in DbEditorTableGroupFieldJson fieldDataSource, List<IDbTableField> fields, object defaultValue)
        {
            FieldDataSource = fieldDataSource;
            Fields = new ObservableCollection<IDbTableField>(fields);
            this.defaultValue = defaultValue;
        }

        public bool CanAddFieldOfType(System.Type fieldType) => typeof(T) == fieldType;

        public object GetDefaultValue() => defaultValue;
    }
}