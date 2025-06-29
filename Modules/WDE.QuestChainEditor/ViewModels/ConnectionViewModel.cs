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
    [Notify] [AlsoNotify(nameof(Text))] private ConnectionType requirementType;

    public BaseQuestViewModel? ToNode => To?.Node;
    public BaseQuestViewModel? FromNode => From?.Node;

    public ConnectionViewModel(ConnectionType type, BaseQuestViewModel? from = null, BaseQuestViewModel? to = null)
    {
        if (from != null && from == to)
            throw new UserException("Can't create loops to self");
        RequirementType = type;
        Attach(from?.Connector, to?.Connector);
        //StackTraces.Add((new StackTrace(true), true));
    }

    public ConnectorViewModel? From => from;

    public ConnectorViewModel? To => to;

    public void Attach(BaseQuestViewModel? from, BaseQuestViewModel? to)
    {
        Attach(from?.Connector, to?.Connector);
    }

    public void Attach(ConnectorViewModel? from, ConnectorViewModel? to)
    {
        if (this.from == from && this.to == to)
            return; // already attached
        if (this.from != null || this.to != null)
            throw new UserException("Connection already attached");
        this.from = from;
        this.to = to;
        using var _ = this.from?.Connections.SuspendNotifications();
        using var __ = this.to?.Connections.SuspendNotifications();
        this.from?.Connections.Add(this);
        this.to?.Connections.Add(this);
        RaisePropertyChanged(nameof(FromNode));
        RaisePropertyChanged(nameof(ToNode));
        RaisePropertyChanged(nameof(From));
        RaisePropertyChanged(nameof(To));
    }

    public void Detach()
    {
        using var _ = from?.Connections.SuspendNotifications();
        using var __ = to?.Connections.SuspendNotifications();
        from?.Connections.Remove(this);
        to?.Connections.Remove(this);
        from = null;
        to = null;
        RaisePropertyChanged(nameof(FromNode));
        RaisePropertyChanged(nameof(ToNode));
        RaisePropertyChanged(nameof(From));
        RaisePropertyChanged(nameof(To));
        //StackTraces.Add((new StackTrace(true), false));
    }

    //public List<(StackTrace, bool creation)> StackTraces { get; } = new();

    public string Text => RequirementType switch
    {
        ConnectionType.Completed => "",
        ConnectionType.MustBeActive => "Active",
        ConnectionType.Breadcrumb => "Optional",
        ConnectionType.FactionChange => "Faction Change",
        _ => throw new ArgumentOutOfRangeException()
    };
}