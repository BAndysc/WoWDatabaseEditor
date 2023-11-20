using System;
using System.Collections.Generic;
using System.Linq;
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
            var definition = tableDefinitionProvider.GetDefinition(item.TableName);
            if (definition == null)
                return Array.Empty<IRemoteCommand>();
            if (definition.ReloadCommand == null)
            {
                if (definition.Condition != null) 
                    return new IRemoteCommand[] {new ReloadRemoteCommand("reload conditions", RemoteCommandPriority.Middle)};

                return Array.Empty<IRemoteCommand>();
            }

            List<IRemoteCommand> commands = new();
            var split = definition.ReloadCommand.Split(',');

            var priority = 0;
            foreach (var cmd in split.Select(x => x.Trim()))
            {
                if (cmd.Contains("{}"))
                {
                    var newCmd = cmd.Replace("{}", string.Join(" ", item.Entries.Select(x => x.Key[0])));
                    commands.Add(new ReloadRemoteCommand(newCmd, (RemoteCommandPriority)priority));
                }
                else
                    commands.Add(new ReloadRemoteCommand(cmd, (RemoteCommandPriority)priority));
                priority++;
            }
            
            if (definition.Condition != null)
                commands.Add(new ReloadRemoteCommand("reload conditions", (RemoteCommandPriority)priority));
            return commands.ToArray();
        }
    }
}