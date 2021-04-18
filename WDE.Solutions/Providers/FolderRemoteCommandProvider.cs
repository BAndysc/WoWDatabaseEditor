using System;
using System.Collections.Generic;
using WDE.Common;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Solutions.Providers
{
    [AutoRegister]
    public class FolderRemoteCommandProvider : ISolutionItemRemoteCommandProvider<SolutionFolderItem>
    {
        private readonly Lazy<ISolutionItemRemoteCommandGeneratorRegistry> registry;

        public FolderRemoteCommandProvider(Lazy<ISolutionItemRemoteCommandGeneratorRegistry> registry)
        {
            this.registry = registry;
        }

        public IRemoteCommand[] GenerateCommand(SolutionFolderItem item)
        {
            List<IRemoteCommand> commands = new();
            foreach (ISolutionItem i in item.Items)
            {
                if (i.IsExportable)
                    commands.AddRange(registry.Value.GenerateCommand(i));
            }

            return commands.ToArray();
        }
    }
}