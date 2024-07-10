using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using AvaloniaStyles.Controls.FastTableView;
using DynamicData.Binding;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Collections;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.QueryParser.Models;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Processing.Processors.Paths.ViewModels;

public partial class SniffWaypointsDocumentViewModel : ObservableBase, IDocument
{
    private readonly ICachedDatabaseProvider databaseProvider;

    private Dictionary<uint, CreaturePathsViewModel> groups = new Dictionary<uint, CreaturePathsViewModel>();
    
    public ObservableCollection<CreaturePathsViewModel> Creatures { get; } =
        new ObservableCollection<CreaturePathsViewModel>();

    [Notify] private int basePathId;
    
    [Notify] [AlsoNotify(nameof(SelectedGroup), nameof(SelectedGuid))] 
    private object? selectedObject;
    
    public CreaturePathsViewModel? SelectedGroup
    {
        get => selectedObject as CreaturePathsViewModel;
        set => selectedObject = value;
    }
    public CreatureGuidViewModel? SelectedGuid
    {
        get => selectedObject as CreatureGuidViewModel;
        set => selectedObject = value;
    }
    
    public INativeTextDocument TextOutput { get; }
    public IParsingSettings ParsingSettings { get; }

    public ITableMultiSelection PointsSelection { get; } = new TableMultiSelection();
    
    public List<ITableColumnHeader> Columns { get; } = new()
    {
        new Column("Point id", 50),
        new Column("X", 50),
        new Column("Y", 50),
        new Column("Z", 50),
        new Column("Dist to previous", 100),
    };

    public class Column : ITableColumnHeader
    {
        public Column(string header, double width)
        {
            Header = header;
            Width = width;
        }

        public string Header { get; set; }
        public double Width { get; set; }
        public bool IsVisible { get; set; } = true;
    }
    
    public ICommand DeleteSelectedWaypointsCommand { get; set; }
    public IAsyncCommand ExecuteSqlSaveSessionCommand { get; set; }
    public IAsyncCommand ExecuteSqlCommand { get; set; }
    
    public SniffWaypointsDocumentViewModel(ICachedDatabaseProvider databaseProvider,
        INativeTextDocument textOutput,
        IParsingSettings parsingSettings,
        ITextDocumentService textDocumentService)
    {
        this.databaseProvider = databaseProvider;
        TextOutput = textOutput;
        ParsingSettings = parsingSettings;
        
        On(() => SelectedGuid, _ =>
        {
            PointsSelection.Clear();
            UpdateQuery();
        });
        On(() => BasePathId, _ =>
        {
            UpdateQuery();
        });
        
        ExecuteSqlSaveSessionCommand = new AsyncAutoCommand(() => textDocumentService.ExecuteSqlSaveSession(TextOutput.ToString(), true));
        ExecuteSqlCommand = new AsyncAutoCommand(() => textDocumentService.ExecuteSql(TextOutput.ToString()));

        DeleteSelectedWaypointsCommand = new DelegateCommand(() =>
        {
            if (SelectedGuid is not { } creature)
                return;

            var itr = PointsSelection.ContainsIterator;
            List<(int, int)> toRemove = new();

            for (var pathIndex = 0; pathIndex < creature.Paths.Count; pathIndex++)
            {
                var path = creature.Paths[pathIndex];
                for (var pointIndex = 0; pointIndex < path.Waypoints.Count; pointIndex++)
                {
                    var point = path.Waypoints[pointIndex];
                    if (!itr.Contains(new VerticalCursor(pathIndex, pointIndex)))
                        continue;
                    toRemove.Add((pathIndex, pointIndex));
                }
            }
            
            foreach (var (pathIndex, pointIndex) in toRemove.OrderByDescending(x => x.Item2))
            {
                creature.Paths[pathIndex].RemoveAt(pointIndex);
            }

            for (var pathIndex = creature.Paths.Count - 1; pathIndex >= 0; pathIndex--)
            {
                var path = creature.Paths[pathIndex];
                if (path.Waypoints.Count == 0)
                    creature.Paths.RemoveAt(pathIndex);
            }

            Vector3? previousPoint = null;
            foreach (var path in creature.Paths)
            {
                foreach (var wp in path.Waypoints)
                {
                    if (previousPoint.HasValue)
                        wp.DistanceToPrevious = (wp.Point - previousPoint.Value).Length();
                    previousPoint = wp.Point;
                }
            }

            PointsSelection.Clear();
            UpdateQuery();
        });
    }
    
    private async Task UpdateQueryAsync(CreatureGuidViewModel creature, int basePathId, CancellationToken token)
    {
        if (ParsingSettings.WaypointsExporter == null)
            return;
        
        var sb = new StringBuilder();

        var movementState = new IWaypointProcessor.UnitMovementState();

        foreach (var pathVM in creature.Paths)
        {
            var path = new IWaypointProcessor.Path();
            var segment = new IWaypointProcessor.Segment(0, 0, new Vec3(), null);
            path.Segments.Add(segment);
            foreach (var wp in pathVM.Waypoints)
            {
                segment.Waypoints.Add(new Vec3(){X=wp.X, Y=wp.Y, Z=wp.Z});
            }
            movementState.Paths.Add(path);
        }
        
        await ParsingSettings.WaypointsExporter.Export(sb, basePathId, creature.Guid, movementState, creature.RandomnessPct / 100);

        if (token.IsCancellationRequested)
            return;
            
        TextOutput.FromString(sb.ToString());
    }

    private CancellationTokenSource updateQueryToken = new CancellationTokenSource();
        
    public void UpdateQuery()
    {
        if (SelectedGuid is not { } creature)
            return;

        updateQueryToken.Cancel();
        updateQueryToken = new CancellationTokenSource();
        UpdateQueryAsync(creature, BasePathId, updateQueryToken.Token).ListenErrors();
    }
    
    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History => null;
    public bool IsModified => false;
    public string Title { get; set; } = "Sniff paths";
    public ImageUri? Icon => new ImageUri("Icons/document_waypoints2.png");
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save => AlwaysDisabledAsyncCommand.Command;
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;

    public async Task AddCreaturePaths(UniversalGuid guid, float randomness, IWaypointProcessor.UnitMovementState paths)
    {
        bool groupCreated = false;
        if (!groups.TryGetValue(guid.Entry, out var group))
        {
            groupCreated = true;
            var template = databaseProvider.GetCachedCreatureTemplate(guid.Entry);
            group = new CreaturePathsViewModel(guid.Entry, template?.Name ?? "Unknown creature");
        }

        var creatureInstance = new CreatureGuidViewModel(this, guid, randomness * 100, paths);
        if (creatureInstance.Paths.Count > 0)
        {
            group.Guids.Add(creatureInstance);

            if (groupCreated)
            {
                groups[guid.Entry] = group;
                Creatures.Add(group);
            }
        }
    }
}

public class CreaturePathsViewModel : ObservableBase
{
    public CreaturePathsViewModel(uint entry, string name)
    {
        Entry = entry;
        Name = name;
    }

    public uint Entry { get; }
    public string Name { get; }
    
    public ObservableCollection<CreatureGuidViewModel> Guids { get; } = new();
}

public class CreatureGuidViewModel : ObservableBase
{
    public CreatureGuidViewModel(SniffWaypointsDocumentViewModel parent, UniversalGuid guid, float randomnessPct, IWaypointProcessor.UnitMovementState movement)
    {
        Parent = parent;
        Guid = guid;
        RandomnessPct = randomnessPct;
        Movement = movement;
        NiceString = guid.ToWowParserString();

        Vector3? previousPoint = null;
        foreach (var path in movement.Paths)
        {
            if (path.Segments.Count == 0)
                continue;

            var initialPosition = path.Segments[0].InitialNpcPosition;
            
            var pathViewModel = new CreaturePathViewModel(this, TimeSpan.FromMilliseconds(path.TotalMoveTime), new Vector3(initialPosition.X, initialPosition.Y, initialPosition.Z));
            int pointId = 1;
            
            using var _ = pathViewModel.Waypoints.SuspendNotifications();
            
            foreach (var waypoint in path.Segments.SelectMany(x => x.Waypoints))
            {
                var point = new Vector3(waypoint.X, waypoint.Y, waypoint.Z);
                
                float distanceToPrevious = 0;
                if (previousPoint.HasValue)
                {
                    distanceToPrevious = (point - previousPoint.Value).Length();
                }
                
                pathViewModel.Waypoints.Add(new CreatureWaypointViewModel()
                {
                    PointId = pointId++,
                    X = waypoint.X,
                    Y = waypoint.Y,
                    Z = waypoint.Z,
                    DistanceToPrevious = distanceToPrevious
                });
                previousPoint = point;
            }
            if (pathViewModel.Waypoints.Count > 1)
                Paths.Add(pathViewModel);
        }
    }

    public SniffWaypointsDocumentViewModel Parent { get; }
    public UniversalGuid Guid { get; }
    public float RandomnessPct { get; }
    public IWaypointProcessor.UnitMovementState Movement { get; }
    public string NiceString { get; }
    public ObservableCollection<CreaturePathViewModel> Paths { get; } = new();

    public void PathVisibilityChanged()
    {
        Parent.UpdateQuery();
    }
}

public partial class CreaturePathViewModel : ObservableBase, ITableRowGroup
{
    private readonly TimeSpan duration;
    private readonly Vector3 initialPosition;
    [Notify] private bool isVisible = true;
    [Notify] private float totalDistance;
    [Notify] private float averageSpeed;
    public string Duration => $"{(int)duration.TotalSeconds} s {(int)duration.Milliseconds} ms";
    
    public FastObservableCollection<CreatureWaypointViewModel> Waypoints { get; } = new();
    public IReadOnlyList<ITableRow> Rows => Waypoints;
    public event Action<ITableRowGroup, ITableRow>? RowChanged;
    public event Action<ITableRowGroup>? RowsChanged;

    public CreaturePathViewModel(CreatureGuidViewModel vm, TimeSpan duration, Vector3 initialPosition)
    {
        this.duration = duration;
        this.initialPosition = initialPosition;
        Waypoints.CollectionChanged += WaypointsOnCollectionChanged;
        
        On(() => IsVisible, _ => vm.PathVisibilityChanged());
    }

    private void WaypointsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Vector3 previousPoint = initialPosition;
        float distance = 0;
        foreach (var p in Waypoints)
        {
            distance += (p.Point - previousPoint).Length();
            previousPoint = p.Point;
        }

        TotalDistance = distance;
        AverageSpeed = distance / (float)duration.TotalSeconds;
    }

    public void RemoveAt(int index)
    {
        Waypoints.RemoveAt(index);
        RowsChanged?.Invoke(this);
    }
}

public class CreatureWaypointViewModel : ObservableBase, ITableRow
{
    public IReadOnlyList<ITableCell> CellsList { get; set; }

    public int PointId { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public Vector3 Point => new(X, Y, Z);
    private float? distanceToPrevious;
    public float? DistanceToPrevious
    {
        get => distanceToPrevious;
        set
        {
            distanceToPrevious = value;
            Changed?.Invoke(this);
        }
    }
    public event Action<ITableRow>? Changed;

    public CreatureWaypointViewModel()
    {
        CellsList = new ITableCell[]
        {
            new IntCell(() => PointId, i => { }),
            new FloatCell(() => X, f => { }),
            new FloatCell(() => Y, f => { }),
            new FloatCell(() => Z, f => { }),
            new StringCell(() => DistanceToPrevious.ToString() ?? "", f => { }),
        };
    }
}