using System;
using System.Collections.Generic;
using WDE.Common.Debugging;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.Debugger.ViewModels.Inspector;

internal partial class SelectedDebugPointViewModel : ObservableBase
{
    private readonly IDebuggerService debuggerService;
    private readonly DebugPointId[] ids;
    private readonly IDebugPointSource source;

    public string Header { get; }

    public string? CodeCompletionRootKey { get; }

    private bool? GetBool(Func<DebugPointId, bool> getter)
    {
        var first = getter(ids[0]);
        for (int i = 1; i < ids.Length; ++i)
            if (getter(ids[i]) != first)
                return null;
        return first;
    }

    private string? GetString(Func<DebugPointId, string> getter)
    {
        var first = getter(ids[0]);
        for (int i = 1; i < ids.Length; ++i)
            if (getter(ids[i]) != first)
                return null;
        return first;
    }

    private void DoForAll(Action<DebugPointId> action)
    {
        foreach (var id in ids)
            action(id);
    }

    public bool? IsDeactivated => GetBool(id => !debuggerService.GetActivated(id));

    public bool? IsEnabled
    {
        get => GetBool(id => debuggerService.GetEnabled(id));
        set
        {
            if (IsEnabled == value)
                return;

            if (value == null)
                return;

            DoForAll(id => debuggerService.SetEnabled(id, value.Value));
            DoForAll(id => debuggerService.Synchronize(id).ListenErrors());
        }
    }

    public bool? SuspendExecution
    {
        get => GetBool(id => debuggerService.GetSuspendExecution(id));
        set
        {
            if (SuspendExecution == value || !CanChangeSuspendExecution)
                return;

            if (value == null)
                return;

            DoForAll(id => debuggerService.SetSuspendExecution(id, value.Value));
            DoForAll(id => debuggerService.Synchronize(id).ListenErrors());
        }
    }

    public bool? Log
    {
        get => GetBool(x => debuggerService.GetLogFormat(x) != null);
        set
        {
            if (value == null)
                return;

            if (value.Value && Log == false)
                DoForAll(id => debuggerService.SetLog(id, ""));
            else if (!value.Value)
                DoForAll(id => debuggerService.SetLog(id, null));
            DoForAll(id => debuggerService.Synchronize(id).ListenErrors());
        }
    }

    public bool? UseLogCondition
    {
        get => GetBool(x => debuggerService.GetLogCondition(x) != null);
        set
        {
            if (value == null)
                return;

            if (value.Value && UseLogCondition == false)
                DoForAll(id => debuggerService.SetLogCondition(id, ""));
            else if (!value.Value)
                DoForAll(id => debuggerService.SetLogCondition(id, null));
            DoForAll(id => debuggerService.Synchronize(id).ListenErrors());
        }
    }

    public string LogCondition
    {
        get => GetString(x => debuggerService.GetLogCondition(x) ?? "") ?? "-";
        set
        {
            if (LogCondition == value)
                return;

            DoForAll(id => debuggerService.SetLogCondition(id, value));
        }
    }

    public string LogFormat
    {
        get => GetString(x => debuggerService.GetLogFormat(x) ?? "") ?? "-";
        set
        {
            if (LogFormat == value)
                return;

            DoForAll(id => debuggerService.SetLog(id, value));
            DoForAll(id => debuggerService.Synchronize(id).ListenErrors());
        }
    }

    public bool? NoStacktrace
    {
        get => GetBool(x => !debuggerService.GetGenerateStacktrace(x));
        set
        {
            if (value == null || !CanChangeGenerateStacktrace)
                return;

            if (NoStacktrace == value)
                return;

            DoForAll(id => debuggerService.SetGenerateStacktrace(id, !value.Value));
            DoForAll(id => debuggerService.Synchronize(id).ListenErrors());
        }
    }

    public object? PayloadViewModel { get; }

    public bool CanChangeSuspendExecution => source.Features.HasFlag(DebugSourceFeatures.CanChangeSuspendExecution);

    public bool CanChangeGenerateStacktrace => source.Features.HasFlag(DebugSourceFeatures.CanChangeGenerateStacktrace);

    public SelectedDebugPointViewModel(IDebuggerService debuggerService,
        DebugPointId[] ids)
    {
        this.debuggerService = debuggerService;
        this.ids = ids;
        if (ids.Length == 0)
            throw new Exception("No ids provided");
        debuggerService.DebugPointChanged += DebugPointChanged;
        source = this.debuggerService.GetSource(ids[0]);
        foreach (var id in ids)
            if (this.debuggerService.GetSource(id) != source)
                throw new Exception("Ids are from different sources");

        CodeCompletionRootKey = source.GetAllowedCodeCompletionsRoot(ids[0]);
        Header = source.GenerateName(ids[0]) + (ids.Length > 1 ? $" (and {ids.Length - 1} more)" : "");
        PayloadViewModel = source.GeneratePayloadViewModel(ids[0]);
    }

    private void DebugPointChanged(DebugPointId obj)
    {
        if (ids.IndexOf(obj) == -1)
            return;

        RaisePropertyChanged(nameof(IsEnabled));
        RaisePropertyChanged(nameof(SuspendExecution));
        RaisePropertyChanged(nameof(Log));
        RaisePropertyChanged(nameof(LogFormat));
        RaisePropertyChanged(nameof(NoStacktrace));
        RaisePropertyChanged(nameof(LogCondition));
        RaisePropertyChanged(nameof(UseLogCondition));
        RaisePropertyChanged(nameof(IsDeactivated));
    }

    public override void Dispose()
    {
        debuggerService.DebugPointChanged -= DebugPointChanged;
        base.Dispose();
    }
}