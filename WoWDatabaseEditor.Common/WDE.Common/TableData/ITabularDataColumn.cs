namespace WDE.Common.TableData;

public interface ITabularDataColumn
{
    public string Header { get; }
    public string PropertyName { get; }
    public float Width { get; }
    public object? DataTemplate { get; }
}