using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

[UniqueProvider]
public interface ITablePersonalSettings
{
    double GetColumnWidth(string table, string column, double defaultWidth);
    bool IsColumnVisible(string table, string column, bool defaultIsVisible = true);
    void UpdateWidth(string table, string column, double defaultWidth, double width);
    void UpdateVisibility(string table, string column, bool isVisible);
}