using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace WoWDatabaseEditorCore.Services.LogService.Logging;

public class LogDataStore : ILogDataStore
{
    #region Methods

    public ObservableCollection<LogModel> Entries { get; } = new();

    public void AddEntry(LogModel logModel)
        => Dispatcher.UIThread.Invoke(() => Entries.Add(logModel));

    #endregion
}