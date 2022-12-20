using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Services.FindAnywhere;

namespace WoWDatabaseEditorCore.Services.OutlinerTool;

public partial class OutlinerSourceViewModel : INotifyPropertyChanged
{
    private readonly IOutlinerSettingsService settings;
    private int enumValue;
    public string Name { get; }
    
    public bool Include
    {
        get => ((int)settings.SkipSources & enumValue) == 0;
        set
        {
            if (value)
                settings.SkipSources &= ~(FindAnywhereSourceType)enumValue;
            else
                settings.SkipSources |= (FindAnywhereSourceType)enumValue;
            OnPropertyChanged();
        }
    }

    public OutlinerSourceViewModel(FindAnywhereSourceType sourceType, IOutlinerSettingsService settings)
    {
        this.settings = settings;
        enumValue = (int)sourceType;
        Name = sourceType.ToString();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}