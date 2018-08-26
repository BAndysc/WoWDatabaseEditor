using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Prism.Ioc;
using WDE.Common.Attributes;

namespace WDE.Common.Solution
{
    public class MetaSolutionSQL : ISolutionItem
    {
        private readonly string _sql;

        public MetaSolutionSQL(string sql)
        {
            _sql = sql;
        }

        public bool IsContainer => false;
        public ObservableCollection<ISolutionItem> Items => null;
        public string ExtraId => null;
        
        public bool IsExportable => false;

        internal string GetSql()
        {
            return _sql;
        }
    }

    [AutoRegister]
    public class MetaSolutionSqlNameProvider : ISolutionNameProvider<MetaSolutionSQL>
    {
        public string GetName(MetaSolutionSQL item)
        {
            return "sql";
        }
    }

    [AutoRegister]
    public class MetaSolutionSQLQueryProvider : ISolutionItemSqlProvider<MetaSolutionSQL>
    {
        public string GenerateSql(MetaSolutionSQL item)
        {
            return item.GetSql();
        }
    }
}
