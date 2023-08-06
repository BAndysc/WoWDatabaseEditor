namespace WDE.QueryGenerators.Base;

public class QueryGeneratorException<T> : Exception
{
    public QueryGeneratorException(string updateName) : base($"{updateName} query generator for " + typeof(T).Name + " not found for selected core version") { }
}