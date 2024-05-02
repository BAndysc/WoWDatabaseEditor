using System;
using Avalonia;
using PropertyChanged.SourceGenerator;
using WDE.MVVM;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.ViewModels;

public partial class PendingConnectionViewModel : ObservableBase
{
    [Notify] private bool isVisible;
    [Notify] private Point targetLocation;

    [Notify] private BaseQuestViewModel? from;
    [Notify] private BaseQuestViewModel? to;

    [Notify] [AlsoNotify(nameof(Text))] private QuestRequirementType requirementType;

    public string Text => RequirementType switch
    {
        QuestRequirementType.Completed => "",
        QuestRequirementType.MustBeActive => "Active",
        QuestRequirementType.Breadcrumb => "Optional",
        _ => throw new ArgumentOutOfRangeException()
    };
}