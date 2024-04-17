using System.Collections.Generic;
using System.Linq;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Services.TabularDataPickerService;

[AutoRegister]
[SingleInstance]
public class TabularDataPickerPreferences : ITabularDataPickerPreferences
{
    private readonly IUserSettings userSettings;
    private Data settings;
    
    public TabularDataPickerPreferences(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        settings = userSettings.Get<Data>();
        settings.ColumnsWidth ??= new();
        settings.WindowSize ??= new();
        
        foreach (var key in settings.ColumnsWidth.Keys.ToList())
        {
            if (settings.ColumnsWidth[key] == null || settings.ColumnsWidth[key].Count == 0)
                settings.ColumnsWidth.Remove(key);
        }
    }
    
    public IReadOnlyDictionary<string, int>? GetSavedColumnsWidth(string key)
    {
        if (settings.ColumnsWidth.TryGetValue(key, out var columns))
            return columns;
        return null;
    }

    public void UpdateColumnsWidth(string key, List<(string, int)> columns)
    {
        settings.ColumnsWidth[key] = columns.ToDictionary(c => c.Item1, c => c.Item2);
        userSettings.Update(settings);
    }

    public void UpdateWindowState(string key, bool maximized, int x, int y, int width, int height)
    {
        settings.WindowSize[key] = new WindowState()
        {
            Maximized = maximized,
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
        userSettings.Update(settings);
    }

    public bool GetWindowState(string table, out bool maximized, out int x, out int y, out int width, out int height)
    {
        if (settings.WindowSize.TryGetValue(table, out var state))
        {
            maximized = state.Maximized;
            x = state.X;
            y = state.Y;
            width = state.Width;
            height = state.Height;
            return true;
        }
        maximized = false;
        x = 0;
        y = 0;
        width = 0;
        height = 0;
        return false;
    }

    public void SetupWindow(string key, IAbstractWindowView window)
    {
        window.OnClosing += _ =>
        {
            var position = window.Position;
            var size = window.LogicalSize; // we use LogicalSize here, because then we set Width directly
            var maximized = window.IsMaximized;
            UpdateWindowState(key, maximized, position.x, position.y, size.x, size.y);
        };
    }
    
    private struct WindowState
    {
        public bool Maximized { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
    
    private struct Data : ISettings
    {
        public Dictionary<string, Dictionary<string, int>> ColumnsWidth { get; set; }
        public Dictionary<string, WindowState> WindowSize { get; set; }
    }
}