using System.Collections.Generic;
using Newtonsoft.Json;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

internal interface IStandaloneTableEditorSettings
{
    bool HasWindowState(DatabaseTable table);
    bool GetWindowState(DatabaseTable table, out bool maximized, out int x, out int y, out int width, out int height);
    void UpdateWindowState(DatabaseTable table, bool maximized, int x, int y, int width, int height);
    void SetupWindow(DatabaseTable table, IAbstractWindowView window);
}

[AutoRegister]
[SingleInstance]
internal class StandaloneTableEditorSettings : IStandaloneTableEditorSettings
{
    private readonly IUserSettings userSettings;
    private Dictionary<DatabaseTable, WindowState> states;
    private Dictionary<IAbstractWindowView, DatabaseTable> windowToTable = new();

    public StandaloneTableEditorSettings(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        var data = userSettings.Get<Data>(new Data())!;
        states = new();
        foreach (var pair in data.LegacyStates)
            states[DatabaseTable.WorldTable(pair.Key)] = pair.Value;
 
        foreach (var pair in data.NewStates)
            states[pair.Key] = pair.Value;
    }

    private struct WindowState
    {
        public bool Maximized { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    private class Data : ISettings
    {
        [JsonProperty("States")]
        public Dictionary<string, WindowState> LegacyStates { get; set; } = new();
        public Dictionary<DatabaseTable, WindowState> NewStates { get; set; } = new();
    }

    public bool HasWindowState(DatabaseTable table)
    {
        return states.ContainsKey(table);
    }

    public bool GetWindowState(DatabaseTable table, out bool maximized, out int x, out int y, out int width, out int height)
    {
        if (states.TryGetValue(table, out var state))
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

    public void UpdateWindowState(DatabaseTable table, bool maximized, int x, int y, int width, int height)
    {
        states[table] = new WindowState()
        {
            Maximized = maximized,
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
        userSettings.Update(new Data(){NewStates = states});
    }

    public void SetupWindow(DatabaseTable table, IAbstractWindowView window)
    {
        windowToTable[window] = table;

        if (GetWindowState(table, out var maximized, out var x, out var y, out var width, out var height) &&
            width > 0 && height > 0)
        {
            window.Reposition(x, y, maximized, width, height);
        }

        window.OnClosing += OnWindowClosing;
    }

    private void OnWindowClosing(IAbstractWindowView window)
    {
        var position = window.Position;
        var size = window.Size;
        var maximized = window.IsMaximized;
        if (windowToTable.TryGetValue(window, out var table))
        {
            UpdateWindowState(table, maximized, position.x, position.y, size.x, size.y);
            windowToTable.Remove(window);
        }

        window.OnClosing -= OnWindowClosing;
    }
}