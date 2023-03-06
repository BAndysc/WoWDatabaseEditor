namespace WDE.Common.TableData;

public class TabularDataColumn : ITabularDataColumn
{
    public TabularDataColumn(string propertyName, string header, float width = 100)
    {
        PropertyName = propertyName;
        Header = header;
        Width = width;
    }

    public string Header { get; }
    public string PropertyName { get; }
    public float Width { get; }
}