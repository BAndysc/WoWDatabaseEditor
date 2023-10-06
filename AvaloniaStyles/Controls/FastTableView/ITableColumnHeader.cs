namespace AvaloniaStyles.Controls.FastTableView;

public interface ITableColumnHeader
{
    string Header { get; }
    double Width { get; set; }
    bool IsVisible { get; }
}

public class TableTableColumnHeader : ITableColumnHeader
{
    public TableTableColumnHeader(string header, double width = 120)
    {
        Header = header;
        Width = width;
    }

    public string Header { get; set; }
    public double Width { get; set; }
    public bool IsVisible { get; set; } = true;
}