namespace WDE.Common.TableData;

public class TabularDataColumn : ITabularDataColumn
{
    public TabularDataColumn(string propertyName, string header, float width = 100, object? dataTemplate = null)
    {
        PropertyName = propertyName;
        Header = header;
        Width = width;
        DataTemplate = dataTemplate;
    }

    public string Header { get; }
    public string PropertyName { get; }
    public float Width { get; }
    public object? DataTemplate { get; }
}