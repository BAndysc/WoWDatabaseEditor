using WDE.Common.History;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.History
{
    public class DbFieldHistoryAction<T> : IHistoryAction
    {
        private readonly DbTableField<T> tableField;
        private readonly T oldValue;
        private readonly T newValue;
        private readonly bool wasModified;
        private readonly bool isModified;

        public DbFieldHistoryAction(DbTableField<T> tableField, T oldValue, T newValue, bool wasModified, bool isModified)
        {
            this.tableField = tableField;
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.wasModified = wasModified;
            this.isModified = isModified;
        }

        public void Undo() => tableField.RevertPropertyValueChange(oldValue, wasModified);

        public void Redo() => tableField.RevertPropertyValueChange(newValue, isModified);

        public string GetDescription() => $"Changed value of field {tableField.FieldName}";
    }
}