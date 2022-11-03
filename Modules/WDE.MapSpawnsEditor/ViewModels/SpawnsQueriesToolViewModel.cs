using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Types;
using WDE.Common.Windows;
using WDE.MapSpawnsEditor.Models;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.MapSpawnsEditor.ViewModels;

[AutoRegister]
public partial class SpawnsQueriesToolViewModel : ObservableBase, ITool
{
    private readonly INativeTextDocument logs;
    private readonly IPendingGameChangesService changes;
    [Notify] private bool visibility;
    [Notify] private bool isSelected;

    public INativeTextDocument LogDocument => logs;
    public string Title => "3D Map Queries Log";
    public string UniqueId => "3dmap_queries_log";
    public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Bottom;
    public bool OpenOnStart => false;
    public ICommand ClearLog { get; }

    public SpawnsQueriesToolViewModel(INativeTextDocument logs, IPendingGameChangesService changes)
    {
        this.logs = logs;
        this.changes = changes;
        ClearLog = new DelegateCommand(() => logs.FromString(""));
        changes.QueryExecuted += q =>
        {
            logs.Append(q.QueryString);
            logs.Append("\n");
        };
    }
}