using System;
using WDE.Common.Services.FindAnywhere;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.OutlinerTool;

[UniqueProvider]
public interface IOutlinerSettingsService
{
    FindAnywhereSourceType SkipSources { get; set; }
    
    event Action? OnSettingsChanged;
}