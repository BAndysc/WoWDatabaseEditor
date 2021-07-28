namespace WDE.SqlQueryGenerator
{
    internal class Table : ITable
    {
        internal Table(string tableName)
        {
            this.TableName = tableName;
        }

        public string TableName { get; }
        public IMultiQuery? CurrentQuery { get; set; }
    }
}