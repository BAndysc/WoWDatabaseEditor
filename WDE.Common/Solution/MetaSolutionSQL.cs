using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;

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
        public string Name => "meta sql";
        public string ExtraId => null;

        public void SetUnity(IUnityContainer unity)
        {
        }

        public bool IsExportable => false;
        public string ExportSql => _sql;
    }
}
