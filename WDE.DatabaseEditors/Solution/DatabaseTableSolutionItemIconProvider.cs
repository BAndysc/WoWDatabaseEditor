using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    [SingleInstance]
    public class DatabaseTableSolutionItemIconProvider : ISolutionItemIconProvider<DatabaseTableSolutionItem>
    {
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private Dictionary<DatabaseTable, ImageUri> definitionToIcon = new();

        public DatabaseTableSolutionItemIconProvider(ITableDefinitionProvider tableDefinitionProvider)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            foreach (var defi in tableDefinitionProvider.Definitions)
            {
                definitionToIcon[defi.Id] = new ImageUri(defi.IconPath ?? "Icons/document.png");
            }
        }
        
        public ImageUri GetIcon(DatabaseTableSolutionItem item)
        {
            if (definitionToIcon.TryGetValue(item.TableName, out var icon))
                return icon;
            return new ImageUri("Icons/document.png");
        }
    }
}