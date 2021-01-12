using System;
using System.Text;
using WDE.Common;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Solutions.Providers
{
    [AutoRegister]
    public class FolderSqlProvider : ISolutionItemSqlProvider<SolutionFolderItem>
    {
        private readonly Lazy<ISolutionItemSqlGeneratorRegistry> registry;

        public FolderSqlProvider(Lazy<ISolutionItemSqlGeneratorRegistry> registry)
        {
            this.registry = registry;
        }

        public string GenerateSql(SolutionFolderItem item)
        {
            StringBuilder sb = new();
            foreach (ISolutionItem i in item.Items)
            {
                if (i.IsExportable)
                    sb.AppendLine(registry.Value.GenerateSql(i));
            }

            return sb.ToString();
        }
    }
}