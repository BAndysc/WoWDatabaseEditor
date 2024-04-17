using WDE.Common.Database;

namespace WDE.QuestChainEditor.ViewModels;

public class QuestListItemViewModel
{
    public QuestListItemViewModel(uint entry, string title, CharacterRaces races, CharacterClasses classes)
    {
        Entry = entry;
        Title = title;
        Races = races;
        Classes = classes;
    }

    public uint Entry { get; }
    public string Title { get; }
    public CharacterRaces Races { get; }
    public CharacterClasses Classes { get; }
}