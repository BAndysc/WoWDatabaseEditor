using WDE.Common.History;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.History
{
    public class DbFieldHistoryAction<T> : IHistoryAction
    {
        private readonly DbTableField<T> tableField;
        private readonly T oldValue;
        private readonly T newValue;

        public DbFieldHistoryAction(DbTableField<T> tableField, T oldValue, T newValue)
        {
            this.tableField = tableField;
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

        public string GetDescription() => $"Changed value of field {tableField.FieldName}";
    }
}