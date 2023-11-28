namespace AvaloniaStyles.Controls.FastTableView;

public interface IRowFilterPredicate
{
    bool IsVisible(ITableRow row, object? parameter);
    bool IsVisible(ITableRowGroup group, object? parameter) { return true; }
}