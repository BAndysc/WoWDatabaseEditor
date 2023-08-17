using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.Settings;

[AutoRegister]
[SingleInstance]
public class DefinitionEditorSettings : IDefinitionEditorSettings
{
    private readonly IUserSettings userSettings;

    private bool introShown;
    public bool IntroShown
    {
        get => introShown;
        set
        {
            introShown = value;
            userSettings.Update(new Data(){Shown = value});
        }
    }

    public DefinitionEditorSettings(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        var data = userSettings.Get<Data>();
        introShown = data.Shown;
    }
    
    private struct Data : ISettings
    {
        public bool Shown;
    }
}