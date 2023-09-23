namespace WDE.Common.DBC.Structs;

public interface ICurrencyCategory
{
    int Id { get; }
    string Name { get; }
}

public class CurrencyCategory : ICurrencyCategory
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}