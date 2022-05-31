namespace WDE.SqlQueryGenerator
{
    public interface IMultiQuery
    {
        void Add(IQuery? query);
        IQuery Close();
        ITable Table(string name);
    }
}