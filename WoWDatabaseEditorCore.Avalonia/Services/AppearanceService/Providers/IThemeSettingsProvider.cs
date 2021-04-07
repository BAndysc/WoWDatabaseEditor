﻿using WDE.Common.Managers;
using WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Data;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Providers
{
    public interface IThemeSettingsProvider
    {
        ThemeSettings GetSettings();
        void UpdateSettings(Theme themeName);
    }
}