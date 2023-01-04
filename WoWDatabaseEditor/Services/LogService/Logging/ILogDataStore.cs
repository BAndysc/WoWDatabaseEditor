using System.Collections.ObjectModel;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.LogService.Logging;

[UniqueProvider]
public interface ILogDataStore
{
    ObservableCollection<LogModel> Entries { get; }
    void AddEntry(LogModel logModel);
}
