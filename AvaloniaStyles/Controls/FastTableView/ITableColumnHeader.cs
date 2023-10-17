using System.ComponentModel;
using PropertyChanged.SourceGenerator;

namespace AvaloniaStyles.Controls.FastTableView;

public interface ITableColumnHeader
{
    string Header { get; }
    double Width { get; set; }
    bool IsVisible { get; }
}

public partial class TableTableColumnHeader : INotifyPropertyChanged, ITableColumnHeader
{
    [Notify] private double width;
    
    public TableTableColumnHeader(string header, double width = 120)
    {
        Header = header;
        Width = width;
    }

    public string Header { get; set; }
    public bool IsVisible { get; set; } = true;
}