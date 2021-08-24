namespace WDE.SqlQueryGenerator
{
    internal class Where : IWhere
    {
        public Where(ITable table, string condition)
        {
            Table = table;
            Condition = condition;
        }

        public ITable Table { get; }
        public string Condition { get; }
    }
}