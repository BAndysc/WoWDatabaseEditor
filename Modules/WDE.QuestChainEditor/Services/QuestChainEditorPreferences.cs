using System.ComponentModel;
using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.QuestChainEditor.Services;

[AutoRegister]
[SingleInstance]
public class QuestChainEditorPreferences : IQuestChainEditorPreferences
{
    private readonly IUserSettings userSettings;
    private Data data;

    public bool AutoLayout
    {
        get => data.AutoLayout;
        set
        {
            data.AutoLayout = value;
            Save();
        }
    }

    public bool NeverShowIncorrectDatabaseDataWarning
    {
        get => data.NeverShowIncorrectDatabaseDataWarning;
        set
        {
            data.NeverShowIncorrectDatabaseDataWarning = value;
            Save();
        }
    }

    public bool HideFactionChangeArrows
    {
        get => data.HideFactionChangeArrows;
        set
        {
            data.HideFactionChangeArrows = value;
            Save();
        }
    }

    private void Save()
    {
        userSettings.Update(data);
    }

    public QuestChainEditorPreferences(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        data = userSettings.Get<Data>(new Data());
    }

    private struct Data : ISettings
    {
        public Data() { }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool AutoLayout { get; set; } = true;

        public bool HideFactionChangeArrows { get; set; } = false;

        public bool NeverShowIncorrectDatabaseDataWarning { get; set; }
    }
}