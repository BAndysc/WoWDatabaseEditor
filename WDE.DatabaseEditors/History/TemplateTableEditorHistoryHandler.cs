using System;
using System.ComponentModel;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.History
{
    public class TemplateTableEditorHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly DbTableData tableData;

        public TemplateTableEditorHistoryHandler(DbTableData tableData)
        {
            this.tableData = tableData;
            BindTableData();
        }
        
        public void Dispose()
        {
            UnbindTableData();
        }

        private void BindTableData()
        {
            foreach (var category in tableData.Categories)
            {
                foreach (var field in category.Fields)
                {
                    if (field is IObservableTableField observableField)
                        observableField.TableFieldValueChanged += FieldNotifierOnPropertyChanged;
                }
            }
        }

        private void UnbindTableData()
        {
            foreach (var category in tableData.Categories)
            {
                foreach (var field in category.Fields)
                {
                    if (field is IObservableTableField observableField)
                        observableField.TableFieldValueChanged -= FieldNotifierOnPropertyChanged;
                }
            }
        }
        
        private void FieldNotifierOnPropertyChanged(object? sender, TableFieldValueChangedEventArgs e)
        {
            if (sender is IObservableTableField field)
                PushAction(new DbFieldHistoryAction(field, e.oldValue, e.newValue, e.wasModified, e.isModified));
        }
    }

    internal class DbFieldHistoryAction : IHistoryAction
    {
        private readonly IObservableTableField tableField;
        private readonly object oldValue;
        private readonly object newValue;
        private readonly bool wasModified;
        private readonly bool isModified;

        internal DbFieldHistoryAction(IObservableTableField tableField, object oldValue, object newValue, bool wasModified, bool isModified)
        {
            this.tableField = tableField;
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.wasModified = wasModified;
            this.isModified = isModified;
        }

        public void Undo() => tableField.RevertPropertyValueChange(oldValue, wasModified);

        public void Redo() => tableField.RevertPropertyValueChange(newValue, isModified);

        public string GetDescription() => $"Changed value of field {"tableField.FieldName"}";
    }
}