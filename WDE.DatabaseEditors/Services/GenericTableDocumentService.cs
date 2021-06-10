using System;
using System.Linq;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services
{
    [AutoRegister]
    [SingleInstance]
    public class GenericTableDocumentService : IGenericTableDocumentService
    {
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private readonly IDocumentManager documentManager;
        private readonly DatabaseTableSolutionItemEditorProvider documentProvider;

        public GenericTableDocumentService(ITableDefinitionProvider tableDefinitionProvider,
            IDocumentManager documentManager,
            DatabaseTableSolutionItemEditorProvider documentProvider)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.documentManager = documentManager;
            this.documentProvider = documentProvider;
        }
        
        public bool TryCreate(string definitionId, string[] columns, uint[] keys, object[][] data)
        {
            var definition = tableDefinitionProvider.GetDefinition(definitionId);
            if (definition == null)
                return false;

            foreach (var column in columns)
            {
                if (!definition.TableColumns.ContainsKey(column))
                    throw new Exception("Definition " + definitionId + " doesn't have column " + column);
            }

            DatabaseTableSolutionItem item = new DatabaseTableSolutionItem(definitionId);
            item.Entries.AddRange(keys.Select(k => new SolutionItemDatabaseEntity(k, true)));

            var doc = documentProvider.GetEditor(item);

            if (doc is not MultiRowDbTableEditorViewModel document)
                throw new Exception("Template not expected here, not yet impemented");

            foreach (var dataRow in data)
            {
                var row = document.AddRow((uint)dataRow[0]);
                for (int i = 1; i < columns.Length; ++i)
                {
                    if (row.GetCell(columns[i]) is DatabaseField<long> lField)
                        lField.Current.Value = (long) dataRow[i];
                    else if (row.GetCell(columns[i]) is DatabaseField<float> fField)
                        fField.Current.Value = (float) dataRow[i];
                    else if (row.GetCell(columns[i]) is DatabaseField<string> sField)
                        sField.Current.Value = (string) dataRow[i];
                }
            }
            documentManager.OpenDocument(document);
            
            return true;
        }
    }
}