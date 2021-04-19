using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.Models
{
    public class DatabaseColumn<T> : IDatabaseColumn
    {
        public string ColumnName => FieldDataSource.Name;
        public string DbColumnName => FieldDataSource.DbColumnName;
        public ObservableCollection<IDatabaseField> Fields { get; }
        public DbEditorTableGroupFieldJson FieldDataSource { get; }
        private object defaultValue;

        public DatabaseColumn(in DbEditorTableGroupFieldJson fieldDataSource, List<IDatabaseField> fields, object defaultValue)
        {
            FieldDataSource = fieldDataSource;
            Fields = new ObservableCollection<IDatabaseField>(fields);
            this.defaultValue = defaultValue;
        }

        public bool CanAddFieldOfType(System.Type fieldType) => typeof(T) == fieldType;

        public object GetDefaultValue() => defaultValue;
    }
}