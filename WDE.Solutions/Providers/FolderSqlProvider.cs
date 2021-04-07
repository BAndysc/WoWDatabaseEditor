using System;
using System.Text;
using System.Threading.Tasks;
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

        public async Task<string> GenerateSql(SolutionFolderItem item)
        {
            StringBuilder sb = new();
            foreach (ISolutionItem i in item.Items)
            {
                if (i.IsExportable)
                    sb.AppendLine(await registry.Value.GenerateSql(i));
            }

            return sb.ToString();
        }
    }
}