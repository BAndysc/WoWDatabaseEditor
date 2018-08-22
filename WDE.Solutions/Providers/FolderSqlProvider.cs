using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Solution;

namespace WDE.Solutions.Providers
{
    internal class FolderSqlProvider : ISolutionItemSqlProvider<SolutionFolderItem>
    {
        private readonly ISolutionItemSqlGeneratorRegistry registry;

        public FolderSqlProvider(ISolutionItemSqlGeneratorRegistry registry)
        {
            this.registry = registry;
        }

        public string GenerateSql(SolutionFolderItem item)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var i in item.Items)
                if (i.IsExportable)
                    sb.AppendLine(i.ExportSql(registry));
            return sb.ToString();
        }
    }
}
