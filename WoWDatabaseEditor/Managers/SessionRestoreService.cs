using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Services.FileSystemService;
using WoWDatabaseEditorCore.Settings;

namespace WoWDatabaseEditorCore.Managers;

[AutoRegister]
[SingleInstance]
public class SessionRestoreService
{
    private static readonly TimeSpan SessionRestoreInterval = TimeSpan.FromSeconds(60);
    private const string LastSessionFile = "~/last_session.json";
   
    private readonly IDocumentManager documentManager;
    private readonly ISolutionItemSerializerRegistry serializerRegistry;
    private readonly ISolutionItemDeserializerRegistry deserializerRegistry;
    private readonly ITaskRunner taskRunner;
    private readonly IEventAggregator eventAggregator;
    private readonly IMessageBoxService messageBoxService;
    private readonly IStatusBar statusBar;
    private readonly IVirtualFileSystem vfs;
    private readonly IGeneralEditorSettingsProvider settings;
    private readonly FileInfo lastSessionFile;
    
    public SessionRestoreService(IDocumentManager documentManager,
        ISolutionItemSerializerRegistry serializerRegistry,
        ISolutionItemDeserializerRegistry deserializerRegistry,
        ITaskRunner taskRunner,
        IEventAggregator eventAggregator,
        IMessageBoxService messageBoxService,
        IStatusBar statusBar,
        IVirtualFileSystem vfs,
        IGeneralEditorSettingsProvider settings)
    {
        this.documentManager = documentManager;
        this.serializerRegistry = serializerRegistry;
        this.deserializerRegistry = deserializerRegistry;
        this.taskRunner = taskRunner;
        this.eventAggregator = eventAggregator;
        this.messageBoxService = messageBoxService;
        this.statusBar = statusBar;
        this.vfs = vfs;
        this.settings = settings;
        lastSessionFile = vfs.ResolvePath(LastSessionFile);
        if (lastSessionFile.Exists && this.settings.RestoreOpenTabsMode != RestoreOpenTabsMode.NeverRestore)
        {
            if (this.settings.RestoreOpenTabsMode == RestoreOpenTabsMode.RestoreWhenCrashed)
                ShowMessage();
            taskRunner.ScheduleTask("Restore last session", async () => Restore());
        }
        Run().ListenErrors();
    }

    private void ShowMessage()
    {
        messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Restore last session")
            .SetMainInstruction("The editor wasn't properly closed")
            .SetContent(
                "Unfortunately the editor wasn't properly closed last time. The recently opened documents will be restored.")
            .WithOkButton(true)
            .Build()).ListenErrors();
    }

    public void GracefulShutdown()
    {
        if (settings.RestoreOpenTabsMode is RestoreOpenTabsMode.RestoreWhenCrashed or RestoreOpenTabsMode.NeverRestore)
            lastSessionFile.Delete();
        else
            Snapshot();
    }

    private void Restore()
    {
        try
        {
            var content = File.ReadAllText(lastSessionFile.FullName);
            var snapshots = JsonConvert.DeserializeObject<List<SavedSnapshot>>(content);
            if (snapshots == null)
                return;
            
            foreach (var snapshot in snapshots)
            {
                if (deserializerRegistry.TryDeserialize(snapshot.Item, out var solutionItem) && solutionItem != null)
                {
                    try
                    {
                        var editor = documentManager.OpenDocument(solutionItem);
                        if (editor is IPeriodicSnapshotDocument psd && snapshot.State != null)
                            psd.RestoreSnapshot(snapshot.State);
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }
            }
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            lastSessionFile.Delete();
        }
    }
    
    private async Task Run()
    {
        while (true)
        {
            await Task.Delay(SessionRestoreInterval);

            Snapshot();
        }
    }

    private void Snapshot()
    {
        if (settings.RestoreOpenTabsMode == RestoreOpenTabsMode.NeverRestore)
            return;
            
        List<SavedSnapshot> items = new();
        foreach (var doc in documentManager.OpenedDocuments)
        {
            if (doc is ISolutionItemDocument si)
            {
                string? state = null;
                if (doc is IPeriodicSnapshotDocument psd)
                {
                    state = psd.TakeSnapshot();
                }

                try
                {
                    var serialized = serializerRegistry.Serialize(si.SolutionItem, true);
                    if (serialized == null)
                        continue;
                    var item = new AbstractSmartScriptProjectItem(serialized);
                    items.Add(new SavedSnapshot() { Item = item, State = state });
                }
                catch (Exception e)
                {
                    LOG.LogError(e, message: "Couldn't serialize " + si.SolutionItem);
                }
            }
        }

        File.WriteAllText(lastSessionFile.FullName, JsonConvert.SerializeObject(items));
        statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Session saved"));
    }

    public struct SavedSnapshot
    {
        public AbstractSmartScriptProjectItem Item { get; set; }
        public string? State { get; set; }
    }
}