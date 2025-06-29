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

    [Notify] [AlsoNotify(nameof(Text))] private ConnectionType requirementType;

    public string Text => RequirementType switch
    {
        ConnectionType.Completed => "",
        ConnectionType.MustBeActive => "Active",
        ConnectionType.Breadcrumb => "Optional",
        ConnectionType.FactionChange => "Faction Change",
        _ => throw new ArgumentOutOfRangeException()
    };
}

public enum ConnectionType
{
    Completed,
    MustBeActive,
    Breadcrumb,
    FactionChange
}

public static class ConnectionTypeExtensions
{
    public static QuestRequirementType ToRequirementType(this ConnectionType type)
    {
        return type switch
        {
            ConnectionType.Completed => QuestRequirementType.Completed,
            ConnectionType.MustBeActive => QuestRequirementType.MustBeActive,
            ConnectionType.Breadcrumb => QuestRequirementType.Breadcrumb,
            ConnectionType.FactionChange => throw new Exception("This is not a requirement type, you should have a WHERE clause for it in the query"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static ConnectionType ToConnectionType(this QuestRequirementType type)
    {
        return type switch
        {
            QuestRequirementType.Completed => ConnectionType.Completed,
            QuestRequirementType.MustBeActive => ConnectionType.MustBeActive,
            QuestRequirementType.Breadcrumb => ConnectionType.Breadcrumb,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}