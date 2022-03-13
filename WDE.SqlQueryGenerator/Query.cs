namespace WDE.SqlQueryGenerator
{
    internal class Query : IQuery
    {
        public Query( string query)
        {
            QueryString = query;
        }

        public Query(ITable table, string query)
        {
            table?.CurrentQuery?.Add(this);
            QueryString = query;
        }
        
        internal Query(IMultiQuery multiQuery, string query)
        {
            multiQuery.Add(this);
            QueryString = query;
        }

        public string QueryString { get; }

        public override string ToString() => QueryString;
    }
}