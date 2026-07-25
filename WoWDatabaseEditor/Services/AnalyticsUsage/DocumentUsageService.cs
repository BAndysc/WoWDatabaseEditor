using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using WDE.Common;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.AnalyticsUsage;

/// <summary>
/// Tracks which documents/tools are opened and how much time each kind of document
/// is the active one. Everything goes through the usage counters, so opens are
/// reported as per-window counts and active time as per-window "document_time"
/// events with a "count" property holding the seconds.
/// </summary>
[AutoRegister]
[SingleInstance]
public class DocumentUsageService : IDisposable
{
    private static readonly TimeSpan AccrueInterval = TimeSpan.FromMinutes(1);
    // a document focused for longer than this without any switch is assumed to be an editor left open (idle)
    private static readonly TimeSpan MaxContinuousFocus = TimeSpan.FromHours(1);

    private readonly IDocumentManager documentManager;
    private readonly IAnalyticsService analytics;
    private readonly IEditorFocusService focusService;

    private readonly IDisposable timer;
    private string? activeDocumentName;
    private bool editorFocused;
    private DateTime lastAccrueTime;
    private DateTime focusStartTime;

    // distinct solution items the user actually modified this session, per document kind;
    // used only to deduplicate - the first modification of an entry counts +1 into the
    // "entries_edited" windowed counter, the items themselves never leave the machine
    private readonly Dictionary<string, HashSet<ISolutionItem>> editedEntries = new();
    private readonly Dictionary<IDocument, (IHistoryManager history, PropertyChangedEventHandler handler)> watchedDocuments = new();

    public DocumentUsageService(IDocumentManager documentManager,
        IAnalyticsService analytics,
        IEditorFocusService focusService,
        IMainThread mainThread)
    {
        this.documentManager = documentManager;
        this.analytics = analytics;
        this.focusService = focusService;

        lastAccrueTime = focusStartTime = DateTime.UtcNow;
        activeDocumentName = UsageNameOf(documentManager.ActiveDocument);
        editorFocused = focusService.IsEditorFocused;

        documentManager.PropertyChanged += OnDocumentManagerPropertyChanged;
        documentManager.OpenedDocuments.CollectionChanged += OnOpenedDocumentsChanged;
        documentManager.OpenedTools.CollectionChanged += OnOpenedToolsChanged;
        focusService.FocusChanged += OnEditorFocusChanged;
        timer = mainThread.StartTimer(OnTick, AccrueInterval);

        foreach (var document in documentManager.OpenedDocuments)
            WatchDocumentModifications(document);
    }

    public void Dispose()
    {
        timer.Dispose();
        documentManager.PropertyChanged -= OnDocumentManagerPropertyChanged;
        documentManager.OpenedDocuments.CollectionChanged -= OnOpenedDocumentsChanged;
        documentManager.OpenedTools.CollectionChanged -= OnOpenedToolsChanged;
        focusService.FocusChanged -= OnEditorFocusChanged;
        foreach (var watched in watchedDocuments.Values)
            watched.history.PropertyChanged -= watched.handler;
        watchedDocuments.Clear();
    }

    private void OnEditorFocusChanged(bool focused)
    {
        // close the interval under the previous focus state first
        Accrue();
        editorFocused = focused;
        if (focused)
            focusStartTime = DateTime.UtcNow; // coming back to the editor is user activity, reset the idle stretch
    }

    internal void FlushOnExit()
    {
        Accrue();
    }

    private void OnDocumentManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IDocumentManager.ActiveDocument))
            return;

        Accrue();
        activeDocumentName = UsageNameOf(documentManager.ActiveDocument);
        focusStartTime = DateTime.UtcNow;
    }

    private void OnOpenedDocumentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var document in e.OldItems)
            {
                if (document is IDocument doc)
                    UnwatchDocumentModifications(doc);
            }
        }

        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null)
            return;

        foreach (var document in e.NewItems)
        {
            if (UsageNameOf(document) is { } name)
                analytics.CountEvent("document_opened", ("document", name));
            if (document is IDocument doc)
                WatchDocumentModifications(doc);
        }
    }

    private void WatchDocumentModifications(object? document)
    {
        if (document is not ISolutionItemDocument { History: { } history } solutionDocument)
            return;

        if (watchedDocuments.ContainsKey(solutionDocument))
            return;

        PropertyChangedEventHandler handler = (_, args) =>
        {
            if (args.PropertyName == nameof(IHistoryManager.CanUndo) && history.CanUndo)
                MarkEntryEdited(solutionDocument);
        };
        history.PropertyChanged += handler;
        watchedDocuments[solutionDocument] = (history, handler);

        if (history.CanUndo)
            MarkEntryEdited(solutionDocument);
    }

    private void UnwatchDocumentModifications(IDocument document)
    {
        if (!watchedDocuments.Remove(document, out var watched))
            return;

        watched.history.PropertyChanged -= watched.handler;
    }

    private void MarkEntryEdited(ISolutionItemDocument document)
    {
        UnwatchDocumentModifications(document);

        if (UsageNameOf(document) is not { } name)
            return;

        if (!editedEntries.TryGetValue(name, out var entries))
            entries = editedEntries[name] = new HashSet<ISolutionItem>();
        if (entries.Add(document.SolutionItem))
            analytics.CountEvent("entries_edited", ("document", name));
    }

    private void OnOpenedToolsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null)
            return;

        foreach (var tool in e.NewItems)
        {
            if (UsageNameOf(tool) is { } name)
                analytics.CountEvent("tool_opened", ("tool", name));
        }
    }

    private bool OnTick()
    {
        Accrue();
        return true;
    }

    private void Accrue()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - lastAccrueTime;
        lastAccrueTime = now;

        if (activeDocumentName == null || !editorFocused)
            return;

        // the timer not ticking for a long time means the machine was asleep, don't count that time
        if (elapsed > AccrueInterval * 2)
            elapsed = AccrueInterval * 2;

        if (now - focusStartTime > MaxContinuousFocus)
            return;

        analytics.CountEvent("document_time", elapsed.TotalSeconds, ("document", activeDocumentName));
    }

    private static string? UsageNameOf(object? documentOrTool)
    {
        if (documentOrTool == null)
            return null;

        if (documentOrTool is IAnalyticsNameProvider { AnalyticsName: { } custom })
            return custom;

        var typeName = documentOrTool.GetType().Name;
        const string suffix = "ViewModel";
        if (typeName.EndsWith(suffix, StringComparison.Ordinal) && typeName.Length > suffix.Length)
            typeName = typeName[..^suffix.Length];
        return typeName;
    }
}
