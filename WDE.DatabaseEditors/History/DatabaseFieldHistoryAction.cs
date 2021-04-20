using WDE.Common.History;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.History
{
    public class DatabaseFieldHistoryAction<T> : IHistoryAction
    {
        private readonly DatabaseField<T> tableField;
        private readonly string property;
        private readonly T oldValue;
        private readonly T newValue;

        public DatabaseFieldHistoryAction(DatabaseField<T> tableField, string property, T oldValue, T newValue)
        {
            this.tableField = tableField;
            this.property = property;
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        public void Undo()
        {
            tableField.Parameter.Value = oldValue;
        }

        public void Redo()
        {
            tableField.Parameter.Value = newValue;
        }

        public string GetDescription() => $"Changed value of {property} to {newValue}";
    }
}