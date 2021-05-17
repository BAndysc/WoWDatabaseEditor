using WDE.Common.History;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.History
{
    public class DatabaseFieldHistoryAction<T> : IHistoryAction
    {
        private readonly DatabaseField<T> tableField;
        private readonly string property;
        private readonly T? oldValue;
        private readonly T? newValue;
        private bool wasNull;
        private bool isNull;

        public DatabaseFieldHistoryAction(DatabaseField<T> tableField, string property, T? oldValue, T? newValue, bool wasNull, bool isNull)
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
    }
}