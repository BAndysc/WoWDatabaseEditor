using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WDE.DatabaseEditors.Models;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.Data
{
    public class DbTableFieldNameSwapHandler : IDbTableFieldNameSwapHandler, IDisposable
    {
        private readonly DbTableData tableData;
        private readonly TableFieldsNameSwapDefinition nameSwapDefinition;

        public DbTableFieldNameSwapHandler(DbTableData tableData, TableFieldsNameSwapDefinition nameSwapDefinition)
        {
            this.tableData = tableData;
            this.nameSwapDefinition = nameSwapDefinition;
            BindFields();
        }

        public void Dispose()
        {
            UnbindFields();
        }
        
        private void BindFields()
        {
            var keyField = tableData.Categories.SelectMany(c => c.Fields)
                .First(f => f.FieldMetaData.DbColumnName == nameSwapDefinition.ConditionValueSource);
            if (keyField is ISwappableNameField swappableNameField)
                swappableNameField.RegisterNameSwapHandler(this);
        }

        private void UnbindFields()
        {
            var keyField = tableData.Categories.SelectMany(c => c.Fields)
                .First(f => f.FieldMetaData.DbColumnName == nameSwapDefinition.ConditionValueSource);
            if (keyField is ISwappableNameField swappableNameField)
                swappableNameField.UnregisterNameSwapHandler();
        }

        public void OnFieldValueChanged(long newValue, string fieldName)
        {
            // just for sure
            if (fieldName != nameSwapDefinition.ConditionValueSource)
                return;

            if (nameSwapDefinition.Options.ContainsKey(newValue))
                UpdateFields(nameSwapDefinition.Options[newValue]);
            else
                RestoreNames();
        }

        private void UpdateFields(IList<TableFieldSwapDataDefinition> definitions)
        {
            foreach (var field in tableData.Categories.SelectMany(c => c.Fields))
            {
                if (!(field is ISwappableNameField swappableNameField))
                    continue;

                try
                {
                    var data = definitions.First(d => d.DbColumnName == field.FieldMetaData.DbColumnName);
                    swappableNameField.UpdateFieldName(data.NewName);
                }
                catch
                {
                }
            }
        }

        private void RestoreNames()
        {
            foreach (var field in tableData.Categories.SelectMany(c => c.Fields))
            {
                if (field is ISwappableNameField swappableNameField)
                    swappableNameField.UpdateFieldName(swappableNameField.OriginalName);
            }
        }
    }
}