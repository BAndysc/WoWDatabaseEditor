using WDE.Module.Attributes;

namespace WDE.QuestChainEditor.Services;

[UniqueProvider]
public interface IQuestChainEditorPreferences
{
    bool AutoLayout { get; set; }
    bool NeverShowIncorrectDatabaseDataWarning { get; set; }
    bool HideFactionChangeArrows { get; set; }
}