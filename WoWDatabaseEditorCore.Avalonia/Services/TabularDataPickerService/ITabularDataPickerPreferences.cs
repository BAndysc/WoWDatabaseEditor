using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Services.TabularDataPickerService;

[UniqueProvider]
public interface ITabularDataPickerPreferences
{
    IReadOnlyDictionary<string, int>? GetSavedColumnsWidth(string key);
    void UpdateColumnsWidth(string key, List<(string, int)> columns);
    void UpdateWindowState(string key, bool maximized, int x, int y, int width, int height);
    bool GetWindowState(string table, out bool maximized, out int x, out int y, out int width, out int height);
    void SetupWindow(string key, IAbstractWindowView window);
}