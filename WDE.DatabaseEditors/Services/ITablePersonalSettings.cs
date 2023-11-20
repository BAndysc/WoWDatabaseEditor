using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

[UniqueProvider]
public interface ITablePersonalSettings
{
    double GetColumnWidth(DatabaseTable table, string column, double defaultWidth);
    bool IsColumnVisible(DatabaseTable table, string column, bool defaultIsVisible = true);
    void UpdateWidth(DatabaseTable table, string column, double defaultWidth, double width);
    void UpdateVisibility(DatabaseTable table, string column, bool isVisible);
}