using System;
using System.Collections.Generic;
using PropertyChanged.SourceGenerator;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.QuestChainEditor.Services;

namespace WDE.QuestChainEditor.ViewModels;

public partial class QuestViewModel : BaseQuestViewModel
{
    [Notify] private CharacterClasses classes;
    [Notify] private CharacterRaces races;
    [Notify] [AlsoNotify(nameof(HasConditions))] private IReadOnlyList<ICondition> conditions = Array.Empty<ICondition>();
    [Notify] private QuestStatus playerQuestStatus;
    [Notify] private bool playerCanStart;
    [Notify] private string? playerCanStartChecks;

    public uint Entry { get; }
    public string Name { get; }
    public bool HasConditions => conditions.Count > 0;

    public ExclusiveGroupViewModel? ExclusiveGroup { get; set; }

    public QuestViewModel(QuestChainDocumentViewModel document, IQuestTemplate template, ICurrentCoreVersion currentCoreVersion) : base(document)
    {
        Entry = template.Entry;
        Name = template.Name;
        Header = Entry.ToString();

        if (template.AllowableClasses != CharacterClasses.None &&
            currentCoreVersion.Current.GameVersionFeatures.AllClasses != template.AllowableClasses)
        {
            Classes = template.AllowableClasses;
        }

        Races = template.AllowableRaces;
    }

    public override string ToString()
    {
        return $"{Name} ({Entry})";
    }

    public override int EntryOrExclusiveGroupId => (int) Entry;
}
//
// public class QuestInputConnectorViewModel : InputConnectorViewModel
// {
//     public QuestInputConnectorViewModel(BaseQuestViewModel node) : base(node)
//     {
//     }
// }
//
// public class QuestOutputConnectorViewModel : OutputConnectorViewModel
// {
//     public QuestOutputConnectorViewModel(BaseQuestViewModel node) : base(node)
//     {
//     }
// }