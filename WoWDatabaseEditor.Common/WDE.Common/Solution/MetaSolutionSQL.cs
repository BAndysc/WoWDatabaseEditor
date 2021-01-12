using System.Collections.ObjectModel;

namespace WDE.Common.Solution
{
    public class MetaSolutionSQL : ISolutionItem
    {
        private readonly string sql;

        public MetaSolutionSQL(string sql)
        {
            this.sql = sql;
        }

        public bool IsContainer => false;
        public ObservableCollection<ISolutionItem> Items => null;
        public string ExtraId => null;

        public bool IsExportable => false;

        public string GetSql()
        {
            return sql;
        }
    }
}