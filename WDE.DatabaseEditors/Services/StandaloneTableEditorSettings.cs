using System.Collections.Generic;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services;

internal interface IStandaloneTableEditorSettings
{
    bool HasWindowState(string table);
    bool GetWindowState(string table, out bool maximized, out int x, out int y, out int width, out int height);
    void UpdateWindowState(string table, bool maximized, int x, int y, int width, int height);
}

[AutoRegister]
[SingleInstance]
internal class StandaloneTableEditorSettings : IStandaloneTableEditorSettings
{
    private readonly IUserSettings userSettings;
    private Data settings;

    public StandaloneTableEditorSettings(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        settings = userSettings.Get<Data>(new Data())!;
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
        public Dictionary<string, WindowState> States { get; set; } = new Dictionary<string, WindowState>();
    }

    public bool HasWindowState(string table)
    {
        return settings.States.ContainsKey(table);
    }

    public bool GetWindowState(string table, out bool maximized, out int x, out int y, out int width, out int height)
    {
        if (settings.States.TryGetValue(table, out var state))
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

    public void UpdateWindowState(string table, bool maximized, int x, int y, int width, int height)
    {
        settings.States[table] = new WindowState()
        {
            Maximized = maximized,
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
        userSettings.Update(settings);
    }
}