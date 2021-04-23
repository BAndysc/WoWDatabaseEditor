using System;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Utils;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemRemoteCommandProvider : ISolutionItemRemoteCommandProvider<DatabaseTableSolutionItem>
    {
        private readonly ITableDefinitionProvider tableDefinitionProvider;

        public DatabaseTableSolutionItemRemoteCommandProvider(ITableDefinitionProvider tableDefinitionProvider)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
        }

        public IRemoteCommand[] GenerateCommand(DatabaseTableSolutionItem item)
        {
            var definition = tableDefinitionProvider.GetDefinition(item.DefinitionId);
            if (definition == null || definition.ReloadCommand == null)
                return Array.Empty<IRemoteCommand>();

            return new IRemoteCommand[] {new ReloadRemoteCommand(definition.ReloadCommand)};
        }
    }
}