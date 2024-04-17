using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia;
using PropertyChanged.SourceGenerator;
using WDE.MVVM;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.Services;

namespace WDE.QuestChainEditor.ViewModels;

public abstract partial class BaseQuestViewModel : ObservableBase
{
    [Notify] private string header = "";
    [Notify] private bool isDragging;
    [Notify] private bool isSelected;
    [Notify] private bool isProblematic;
    [Notify] private Point anchor;
    [Notify] private double perfectX;
    [Notify] private double perfectY;

    private Rect bounds;
    private Point location;

    public Rect Bounds
    {
        get => bounds;
        set
        {
            var oldSize = bounds.Size;
            var newSize = value.Size;
            if (newSize.Width == 0 && newSize.Height == 0)
                return; // detached from the visual tree
            bounds = value;
            RaisePropertyChanged();
            if ((int)oldSize.Width != (int)newSize.Width || (int)oldSize.Height != (int)newSize.Height)
            {
                if (this is QuestViewModel qvm && qvm.ExclusiveGroup is { } group)
                    group.Arrange(default);
                Document.ScheduleReLayoutGraph();
            }
        }
    }

    public Point Location
    {
        get => location;
        set
        {
            if (!EqualityComparer<Point>.Default.Equals(value, location))
            {
                location = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(X));
                RaisePropertyChanged(nameof(Y));
                if (Connector.Empty &&
                    (Connector.Node is QuestViewModel ||
                     Connector.Node is ExclusiveGroupViewModel group &&
                        group.Quests.All(q => q.Connector.Empty)))
                {
                    PerfectX = X;
                    PerfectY = Y;
                }
            }
        }
    }

    public double X => Location.X;

    public double Y => Location.Y;

    public Vector2 Force { get; set; }

    protected ConnectorViewModel AddConnector()
    {
        ConnectorViewModel inputConnector = new((BaseQuestViewModel)this);
        return inputConnector;
    }

    public QuestChainDocumentViewModel Document { get; }
    //public QuestInputConnectorViewModel RequiresConnector { get; }
    //public QuestOutputConnectorViewModel IsRequiredByConnector { get; }

    public ConnectorViewModel Connector { get; }
    //public ConnectorViewModel IsRequiredByConnector => Connector;
    //public ConnectorViewModel RequiresConnector => Connector;

    public IEnumerable<(BaseQuestViewModel prerequisite, QuestRequirementType requirementType, ConnectionViewModel conn)> Prerequisites =>
        Connector.Connections.Where(conn => conn.ToNode == this && conn.FromNode != null)
            .Select(x => (x.FromNode!, x.RequirementType, x))
            .ToList();

    public IEnumerable<(BaseQuestViewModel requirementFor, QuestRequirementType requirementType, ConnectionViewModel conn)> RequirementFor =>
        Connector.Connections.Where(conn => conn.FromNode == this && conn.ToNode != null)
            .Select(x => (x.ToNode!, x.RequirementType, x))
            .ToList();

    public abstract int EntryOrExclusiveGroupId { get; }

    public BaseQuestViewModel(QuestChainDocumentViewModel document)
    {
        Document = document;
        Connector = AddConnector();
        //RequiresConnector = AddInputConnector();
        //IsRequiredByConnector = AddOutputConnector();
    }
}