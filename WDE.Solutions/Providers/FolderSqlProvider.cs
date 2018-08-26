using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Attributes;
using WDE.Common.Solution;

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
            StringBuilder sb = new StringBuilder();
            foreach (var i in item.Items)
                if (i.IsExportable)
                    sb.AppendLine(registry.Value.GenerateSql(i));
            return sb.ToString();
        }
    }
}
