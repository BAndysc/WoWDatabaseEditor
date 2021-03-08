using System;

namespace WDE.DatabaseEditors.Models
{
    public interface IObservableTableField
    {
        event TableFieldValueChangedEventHandler TableFieldValueChanged;
        
        void RevertPropertyValueChange(object previousValue, bool previousModified);
    }

    public delegate void TableFieldValueChangedEventHandler(object? sender, TableFieldValueChangedEventArgs e);

    public class  TableFieldValueChangedEventArgs : EventArgs
    {
        public string PropertyName { get; }
        public object oldValue { get; }
        public object newValue { get; }
        public bool wasModified { get; }
        public bool isModified { get; }

        public TableFieldValueChangedEventArgs(string propertyName, object oldValue, bool wasModified, object newValue, bool isModified)
        {
            PropertyName = propertyName;
            this.oldValue = oldValue;
            this.wasModified = wasModified;
            this.newValue = newValue;
            this.isModified = isModified;
        }
    }
}