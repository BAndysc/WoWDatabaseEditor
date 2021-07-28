namespace WDE.SqlQueryGenerator
{
    public static class Queries
    {
        public static IMultiQuery BeginTransaction()
        {
            return new MultiQuery();
        }

        public static ITable Table(string name)
        {
            return new Table(name);
        }

        public static IQuery Empty()
        {
            return new Query(new Table(""), "");
        }
    }
}