namespace AvaloniaStyles.Controls.FastTableView;

public interface ITableColumnHeader
{
    string Header { get; }
    double Width { get; set; }
    bool IsVisible { get; }
}

public class TableTableColumnHeader : ITableColumnHeader
{
    public TableTableColumnHeader(string header)
    {
        Header = header;
    }

    public string Header { get; set; }
    public double Width { get; set; } = 50;
    public bool IsVisible => true;
}