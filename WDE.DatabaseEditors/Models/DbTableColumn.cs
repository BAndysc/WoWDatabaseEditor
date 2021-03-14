using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.DatabaseEditors.Data;

namespace WDE.DatabaseEditors.Models
{
    public class DbTableColumn<T> : IDbTableColumn
    {
        public string ColumnName => FieldDataSource.Name;
        public string DbColumnName => FieldDataSource.DbColumnName;
        public ObservableCollection<IDbTableField> Fields { get; }
        public DbEditorTableGroupFieldJson FieldDataSource { get; }

        public DbTableColumn(in DbEditorTableGroupFieldJson fieldDataSource, List<IDbTableField> fields)
        {
            FieldDataSource = fieldDataSource;
            Fields = new ObservableCollection<IDbTableField>(fields);
        }

        public bool CanAddFieldOfType(System.Type fieldType) => typeof(T) == fieldType;

        public object GetDefaultValue() => default(T);
    }
}