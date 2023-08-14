namespace AvaloniaStyles.Controls.FastTableView;

public interface ITableColumnHeader
{
    string Header { get; }
    double Width { get; set; }
    bool IsVisible { get; }
}