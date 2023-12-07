using System;
using Avalonia.Threading;
using PropertyChanged.SourceGenerator;
using WDE.Common.Tasks;
using WDE.MVVM;
using WDE.SqlWorkbench.Services.ActionsOutput;

namespace WDE.SqlWorkbench.ViewModels;

public partial class ActionOutputViewModel : ObservableBase, IActionOutput
{
    public int Index { get; }
    public DateTime TimeStarted { get; set; }
    public string OriginalQuery { get; }
    [Notify] private string response = "";
    [Notify] [AlsoNotify(nameof(IsFail), nameof(IsSuccess), nameof(DurationAsString))] private ActionStatus status;
    
    private TimeSpan Duration => (TimeFinished ?? DateTime.Now) - TimeStarted;

    private DateTime? timeFinished;
    public DateTime? TimeFinished
    {
        get => timeFinished;
        set
        {
            timeFinished = value;
        }
    }

    public Exception? Exception { get; set; }

    public string TimeAsString => TimeStarted.ToString("HH:mm:ss");
    public string DurationAsString =>
        status == ActionStatus.NotStarted ? "" :
            Duration.TotalMilliseconds > 1000
                ? Duration.TotalSeconds.ToString("0.00") + "s"
                : Duration.TotalMilliseconds.ToString("0.00") + "ms";

    public bool IsFail => status == ActionStatus.Error;
    public bool IsSuccess => status == ActionStatus.Success;

    private string? query;
    public string Query
    {
        get
        {
            if (query != null)
                return query;
            
            if (OriginalQuery.Length > 1024 || OriginalQuery.Contains('\n'))
                query = OriginalQuery.AsSpan(0, Math.Min(1024, OriginalQuery.Length)).ToString().Replace("\n", " ");
            else
                query = OriginalQuery;
            
            return query;
        }
    }
    
    public ActionOutputViewModel(IMainThread mainThread, int index, string query)
    {
        Index = index;
        OriginalQuery = query;
        mainThread.StartTimer(UpdateDuration, TimeSpan.FromMilliseconds(50));
    }

    private bool UpdateDuration()
    {
        RaisePropertyChanged(nameof(DurationAsString));
        return status == ActionStatus.NotStarted || status == ActionStatus.Started;
    }
}