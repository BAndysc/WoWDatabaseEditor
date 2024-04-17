using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Avalonia;
using PropertyChanged.SourceGenerator;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Documents;
using WDE.Common.History;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.WorldMap;
using WDE.WorldMap.Models;
using WDE.WorldMap.Services;
using WDE.WorldMap.ViewModels;

namespace WDE.PathPreviewTool.ViewModels;

public partial class PathPreviewViewModel : ObservableBase, IWizard, IMapContext<PathViewModel>
{
    private readonly IDatabaseProvider databaseProvider;

    public PathPreviewViewModel(IMapDataProvider mapData,
        ITaskRunner taskRunner,
        ICurrentCoreVersion currentCoreVersion,
        IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
        MapData = mapData;

        FetchPaths = new AsyncAutoCommand(async () =>
        {
            if (selectedMap == null)
                return;

            var supportedWaypoints = currentCoreVersion.Current.DatabaseFeatures.SupportedWaypoints;
            
            fetchingPathsToken?.Cancel();
            var token = fetchingPathsToken = new CancellationTokenSource();
            var creatures = await databaseProvider.GetCreaturesByMapAsync(selectedMap.Id);
            var visibleRect = new Rect(TopLeft, BottomRight);

            List<PathViewModel> paths = new();
            foreach (var creature in creatures)
            {
                var editorCoords = CoordsUtils.WorldToEditor(creature.X, creature.Y);
                if (!visibleRect.Contains(new Point(editorCoords.editorX, editorCoords.editorY)))
                    continue;

                if (creature.MovementType != MovementType.Waypoint)
                    continue;

                if (token.IsCancellationRequested)
                    break;

                if (supportedWaypoints.HasFlagFast(WaypointTables.WaypointData))
                {
                    var addon = (IBaseCreatureAddon?)await databaseProvider.GetCreatureAddon(creature.Entry, creature.Guid);

                    if (addon == null)
                        addon = await databaseProvider.GetCreatureTemplateAddon(creature.Entry);

                    if (addon == null || addon.PathId == 0)
                        continue;
                
                    var path = await databaseProvider.GetWaypointData(addon.PathId);
                    if (path != null)
                        paths.Add(new PathViewModel(creature, path));   
                }
                else if (supportedWaypoints.HasFlagFast(WaypointTables.MangosCreatureMovement))
                {
                    var path = (IReadOnlyList<IWaypoint>?)await databaseProvider.GetMangosCreatureMovement(creature.Guid);
                    if (path == null || path.Count == 0)
                        path = await databaseProvider.GetMangosCreatureMovementTemplate(creature.Entry, 0);
                    
                    if (path != null)
                        paths.Add(new PathViewModel(creature, path));   
                }
            }

            if (token == fetchingPathsToken)
            {
                VisibleItems = paths;
                RequestRender?.Invoke();
                fetchingPathsToken = null;
            }
        });
        
        AutoDispose(this
            .ToObservable(x => x.TopLeft)
            .Select(_ => 1)
            .Merge(this.ToObservable(x => x.SelectedMap).Select(_ => 1))
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ =>
            {
                if (autoFetch)
                    FetchPaths.Execute(null);
            }));
        
        taskRunner.ScheduleTask("Load maps", DownloadMaps);
    }

    [Notify] private MapViewModel? selectedMap = null;
    [Notify] private double downloadMapProgress = 0;
    [Notify] private bool isDownloadingMapData;
    [Notify] private bool autoFetch = true;
    private CancellationTokenSource? fetchingPathsToken;
    [Notify] private Point topLeft;
    [Notify] private Point bottomRight;
    
    public AsyncAutoCommand FetchPaths { get; }
    
    public IMapDataProvider MapData { get; }
    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History => null;
    public bool IsModified => false;
    public string Title => "Path preview tool";
    public ImageUri? Icon => new ImageUri("Icons/document_minimap.png");
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save => AlwaysDisabledAsyncCommand.Command;
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public void Center(double x, double y) => RequestCenter?.Invoke(x, y);

    public event Action? RequestRender;
    public event Action<double, double>? RequestCenter;
    public event Action<double, double, double, double>? RequestBoundsToView;
    public void Initialized() { }

    public IEnumerable<PathViewModel> VisibleItems { get; set; } = Enumerable.Empty<PathViewModel>();
    public PathViewModel? SelectedItem { get; set; }

    public void Move(PathViewModel item, double x, double y) { }

    private async Task DownloadMaps(ITaskProgress progress)
    {
        await MapData.LoadMaps(new Progress<(long, long?)>(pair =>
        {
            IsDownloadingMapData = true;
            if (pair.Item2.HasValue)
                DownloadMapProgress = 100.0 * pair.Item1 / pair.Item2.Value;
            else
                DownloadMapProgress = -1;
            progress.Report((int)pair.Item1, (int)(pair.Item2 ?? -1), null);
        }));
        IsDownloadingMapData = false;
        SelectedMap = MapData.Maps.FirstOrDefault(x => x.Name == "Kalimdor");
    }
}
