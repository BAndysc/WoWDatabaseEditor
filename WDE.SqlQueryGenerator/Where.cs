namespace WDE.SqlQueryGenerator
{
    internal class Where : IWhere
    {
        private string? condition;
        
        public Where(ITable table, string? condition = null)
        {
            Table = table;
            this.condition = condition;
        }

        public ITable Table { get; }
        public string Condition => condition ?? "false";
        public bool IsEmpty => condition == null;
    }
}