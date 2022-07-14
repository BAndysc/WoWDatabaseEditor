using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using PropertyChanged.SourceGenerator;
using WDE.Common.Documents;
using WDE.Common.History;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.WorldMap.Models;
using WDE.WorldMap.Services;

namespace WDE.PathPreviewTool.ViewModels;

public partial class PathPreviewViewModel : ObservableBase, IWizard, IMapContext<PathViewModel>
{
    public PathPreviewViewModel(IMapDataProvider mapData,
        ITaskRunner taskRunner)
    {
        MapData = mapData;
        taskRunner.ScheduleTask("Load maps", DownloadMaps);
    }

    [Notify] private string selectedMapPath = "";
    [Notify] private double downloadMapProgress = 0;
    [Notify] private bool isDownloadingMapData;
    
    public IMapDataProvider MapData { get; }
    private ObservableCollection<PathViewModel> paths = new();
    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History => null;
    public bool IsModified => false;
    public string Title => "Path preview tool";
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public ICommand Save => AlwaysDisabledCommand.Command;
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public void Center(double x, double y) => RequestCenter?.Invoke(x, y);

    public event Action? RequestRender;
    public event Action<double, double>? RequestCenter;
    public event Action<double, double, double, double>? RequestBoundsToView;
    public void Initialized()
    {
    }

    public IEnumerable<PathViewModel> VisibleItems => paths;
    public PathViewModel? SelectedItem { get; set; }
    public void Move(PathViewModel item, double x, double y)
    {
    }

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
        SelectedMapPath = MapData.Maps.FirstOrDefault(x => x == "Kalimdor") ?? "";
    }
}