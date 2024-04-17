using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using GraphX.Common.Interfaces;
using GraphX.Measure;
using Newtonsoft.Json.Linq;
using WDE.Common.Settings;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

public abstract class LayoutAlgorithmSettingsViewModel : ObservableBase, ILayoutAlgorithmProvider
{
    // Version of the default settings, if the version is different, the settings will be reset
    public abstract int Version { get; }

    protected LayoutAlgorithmSettingsViewModel(string name)
    {
        Name = name;
        Settings.ToStream(false)
            .SubscribeAction(e =>
            {
                if (e.Type == CollectionEventType.Add)
                {
                    e.Item.PropertyChanged += OnSettingChanged;
                }
            });
    }

    private void OnSettingChanged(object? sender, PropertyChangedEventArgs e)
    {
        SettingsChanged?.Invoke(this);
    }

    public abstract ILayoutAlgorithm<BaseQuestViewModel, AutomaticGraphLayouter.Edge, AutomaticGraphLayouter.Graph>
        Create(AutomaticGraphLayouter.Graph graph, IDictionary<BaseQuestViewModel, Size> vertexSizes,
            IDictionary<BaseQuestViewModel, Point> vertexPositions);

    public virtual bool SeparateByGroups { get; } = true;

    public virtual JObject SaveAsJson()
    {
        JObject o = new JObject();
        o["__version"] = Version;
        foreach (var setting in Settings)
        {
            if (setting is IBoolGenericSetting boolSetting)
            {
                o[boolSetting.Name] = boolSetting.Value;
            }
            else if (setting is IFloatSliderGenericSetting floatSetting)
            {
                o[floatSetting.Name] = floatSetting.Value;
            }
            else if (setting is IListOptionGenericSetting listSetting)
            {
                o[listSetting.Name] = listSetting.Options.IndexOf(listSetting.SelectedOption);
            }
            else
                throw new Exception("Unknown setting type");
        }

        return o;
    }

    public virtual void Load(JObject json)
    {
        foreach (var setting in Settings)
        {
            if (setting is IBoolGenericSetting boolSetting)
            {
                boolSetting.Value = json[boolSetting.Name]?.Value<bool>() ?? boolSetting.Value;
            }
            else if (setting is IFloatSliderGenericSetting floatSetting)
            {
                floatSetting.Value = json[floatSetting.Name]?.Value<float>() ?? floatSetting.Value;
            }
            else if (setting is IListOptionGenericSetting listSetting)
            {
                var index = json[listSetting.Name]?.Value<int>();
                if (index.HasValue && index >= 0 && index < listSetting.Options.Count)
                    listSetting.SelectedOption = listSetting.Options[index.Value];
            }
            else
                throw new Exception("Unknown setting type");
        }
    }

    public ObservableCollection<IGenericSetting> Settings { get; } = new();

    public string Name { get; }

    public event Action<LayoutAlgorithmSettingsViewModel>? SettingsChanged;
}