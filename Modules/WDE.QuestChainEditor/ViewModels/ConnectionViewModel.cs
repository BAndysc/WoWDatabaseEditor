using System;
using System.Collections.Generic;
using System.Diagnostics;
using PropertyChanged.SourceGenerator;
using WDE.Common.Exceptions;
using WDE.MVVM;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.ViewModels;

public partial class ConnectionViewModel : ObservableBase
{
    private ConnectorViewModel? from;
    private ConnectorViewModel? to;
    [Notify] [AlsoNotify(nameof(Text))] private QuestRequirementType requirementType;

    public BaseQuestViewModel? ToNode => To?.Node;
    public BaseQuestViewModel? FromNode => From?.Node;

    public ConnectionViewModel(QuestRequirementType type, BaseQuestViewModel? from = null, BaseQuestViewModel? to = null)
    {
        if (from != null && from == to)
            throw new UserException("Can't create loops to self");
        RequirementType = type;
        From = from?.Connector; // IsRequiredByConnector
        To = to?.Connector; // RequiresConnector
        this.from = from?.Connector;
        this.to = to?.Connector;
        //StackTraces.Add((new StackTrace(true), true));
    }

    public ConnectorViewModel? From
    {
        get => from;
        set
        {
            if (from != null)
                from.Connections.Remove(this);

            from = value;

            if (from != null)
                from.Connections.Add(this);

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(FromNode));
        }
    }

    public ConnectorViewModel? To
    {
        get => to;
        set
        {
            if (to != null)
                to.Connections.Remove(this);

            to = value;

            if (to != null)
                to.Connections.Add(this);

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ToNode));
        }
    }

    public void Detach()
    {
        From = null;
        To = null;
        //StackTraces.Add((new StackTrace(true), false));
    }

    //public List<(StackTrace, bool creation)> StackTraces { get; } = new();

    public string Text => RequirementType switch
    {
        QuestRequirementType.Completed => "",
        QuestRequirementType.MustBeActive => "Active",
        QuestRequirementType.Breadcrumb => "Optional",
        _ => throw new ArgumentOutOfRangeException()
    };
}