using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.Settings;

[UniqueProvider]
public interface IDefinitionEditorSettings
{
    bool IntroShown { get; set; }   
}