using System;
using WDE.Common.History;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.History
{
    public interface IDatabaseFieldHistoryAction : IHistoryAction
    {
        public ColumnFullName Property { get; }
    }
    
    public interface IDatabaseFieldWithKeyHistoryAction : IDatabaseFieldHistoryAction
    {
        public DatabaseKey Key { get; }
    }

    public class DatabaseFieldWithKeyHistoryAction : IDatabaseFieldWithKeyHistoryAction
    {
        private IDatabaseFieldHistoryAction impl;
        private readonly DatabaseKey key;

        public DatabaseFieldWithKeyHistoryAction(IDatabaseFieldHistoryAction impl, DatabaseKey key)
        {
            this.impl = impl;
            this.key = key;
        }

        public void Undo()
        {
            impl.Undo();
        }

        public void Redo()
        {
            impl.Redo();
        }

        public string GetDescription()
        {
            return impl.GetDescription();
        }

        public ColumnFullName Property => impl.Property;

        public DatabaseKey Key => key;
    }
    
    public class DatabaseFieldHistoryAction<T> : IDatabaseFieldHistoryAction where T : IComparable<T>
    {
        private readonly DatabaseField<T> tableField;
        private readonly ColumnFullName property;
        private readonly T? oldValue;
        private readonly T? newValue;
        private readonly bool wasNull;
        private readonly bool isNull;

        public DatabaseFieldHistoryAction(DatabaseField<T> tableField, ColumnFullName property, T? oldValue, T? newValue, bool wasNull, bool isNull)
        {
            this.tableField = tableField;
            this.property = property;
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.wasNull = wasNull;
            this.isNull = isNull;
        }

        public void Undo()
        {
            if (wasNull)
                tableField.Current.SetNull();
            else
                tableField.Current.Value = oldValue;
        }

        public void Redo()
        {
            if (isNull)
                tableField.Current.SetNull();
            else
                tableField.Current.Value = newValue;
        }

        public string GetDescription()
        {
            var value = isNull || newValue == null ? "(null)" : newValue.ToString();
            return $"Changed value of {property} to {value}";
        }

        public ColumnFullName Property => property;
    }
}