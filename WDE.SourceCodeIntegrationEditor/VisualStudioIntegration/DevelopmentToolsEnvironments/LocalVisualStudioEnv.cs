using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE100;
using EnvDTE90a;
using WDE.Common.Database;
using WDE.Common.Debugging;
using WDE.Common.Tasks;
using WDE.SourceCodeIntegrationEditor.Utils;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.COM;
using Thread = EnvDTE.Thread;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.DevelopmentToolsEnvironments;

internal class LocalVisualStudioEnv : IDTE
{
    private static Dictionary<int, string> Versions = new()
    {
        [70] = ".NET 2002", // imagine somebody actually connects to it lol
        [71] = ".NET 2003",
        [80] = "2005",
        [90] = "2008",
        [100] = "2010",
        [110] = "2012",
        [120] = "2013",
        [140] = "2015",
        [150] = "2017",
        [160] = "2019",
        [170] = "2022",
        [180] = "2025", // be future proof :D
        [190] = "2028", // kekw what are the chances
    };

    private readonly IMainThread mainThread;

    private DTE? dte;

    public event EventHandler<dbgEventReason>? RunModeEntered;
    public event EventHandler<IdeBreakpointHitEventArgs>? BreakModeEntered;
    public event EventHandler<dbgEventReason>? DebuggingEnded;

    public LocalVisualStudioEnv(IMainThread mainThread, DTE dte)
    {
        this.mainThread = mainThread;
        this.dte = dte;
    }

    public async Task ConnectAsync()
    {
        // it is crazy, but due to some Avalonia shenanigans,
        // we need to attach to the debugger events in a separate thread
        // interestingly, this is no longer required in Avalonia 11 :shrug:
        var thread = new System.Threading.Thread(() =>
        {
            if (dte == null)
                return;

            MessageFilter.Register();
            this.dte.Events.DebuggerEvents.OnEnterBreakMode += OnEnterBreakMode;
            this.dte.Events.DebuggerEvents.OnEnterRunMode += OnEnterRunMode;
            this.dte.Events.DebuggerEvents.OnContextChanged += OnContextChanged;
            this.dte.Events.DebuggerEvents.OnEnterDesignMode += OnEnterDesignMode;
            MessageFilter.Revoke();
        });
        // The line below is a must have to use COM!!!
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    public bool SkipSolutionPathValidation => false;

    private bool IsDisposed() => dte == null;

    private void OnEnterRunMode(dbgEventReason reason)
    {
        mainThread.Dispatch(() =>
        {
            if (IsDisposed())
                return;

            RunModeEntered?.Invoke(this, reason);
        });
    }

    private void OnEnterDesignMode(dbgEventReason reason)
    {
        mainThread.Dispatch(() =>
        {
            if (IsDisposed())
                return;

            DebuggingEnded?.Invoke(this, reason);
        });
    }

    private static Dictionary<string, int> ScriptTypeNameToId = new Dictionary<string, int>()
    {
        ["SMART_SCRIPT_TYPE_CREATURE"] = 0,
        ["SMART_SCRIPT_TYPE_GAMEOBJECT"] = 1,
        ["SMART_SCRIPT_TYPE_CLIENT_AREATRIGGER"] = 2,
        ["SMART_SCRIPT_TYPE_EVENT"] = 3,
        ["SMART_SCRIPT_TYPE_SCENE"] = 4,
        ["SMART_SCRIPT_TYPE_QUEST"] = 5,
        ["SMART_SCRIPT_TYPE_SPELL"] = 6,
        ["SMART_SCRIPT_TYPE_PLAYER_CHOICE"] = 7,
        ["SMART_SCRIPT_TYPE_INSTANCE"] = 8,
        ["SMART_SCRIPT_TYPE_TIMED_ACTIONLIST"] = 9,
        ["SMART_SCRIPT_TYPE_TEMPLATE"] = 10,
        ["SMART_SCRIPT_TYPE_SPELL_STATIC"] = 11,
        ["SMART_SCRIPT_TYPE_BATTLE_PET"] = 12,
        ["SMART_SCRIPT_TYPE_CONVERSATION"] = 13,
        ["SMART_SCRIPT_TYPE_SERVER_AREATRIGGER"] = 14,
    };

    private static Regex CharRegex = new Regex(@"^(\d)+\s+'\\.'");

    private ConditionBreakpointHitArgs? CreateConditionArgs(string? fileName, StackFrame2 stackFrame2)
    {
        bool TryExtractInt(string? str, out int num)
        {
            if (str == null)
            {
                num = 0;
                return false;
            }

            if (int.TryParse(str, out num))
                return true;

            var indexOfParenthesis = str.IndexOf('(');
            if (indexOfParenthesis != -1)
            {
                var insideParent = str.Substring(indexOfParenthesis + 1, str.Length - indexOfParenthesis - 2);
                return int.TryParse(insideParent, out num);
            }

            if (CharRegex.Match(str) is { Success: true } match)
            {
                var value = match.Groups[1].Value;
                return int.TryParse(value, out num);
            }

            return false;
        }

        if (fileName == null || !fileName.Contains("ConditionMgr"))
            return null;

        var locals = stackFrame2.Locals2[false];
        var thisExpression = Enumerable
            .Range(1, locals.Count)
            .Select(index => (Expression?)locals.Item(index))
            .FirstOrDefault(ex => ex?.Name == "this");

        if (thisExpression == null)
            return null;

        var innerMembers = thisExpression.DataMembers;
        var innerMembersMap = Enumerable.Range(0, innerMembers.Count)
            .Select(x => innerMembers.Item(x + 1))
            .Select((x, index) => (x.Name, index: index + 1))
            .ToDictionary(x => x.Name, x => x.index);

        if (!innerMembersMap.TryGetValue("SourceType", out var sourceTypeIndex) ||
            !innerMembersMap.TryGetValue("SourceGroup", out var sourceGroupIndex) ||
            !innerMembersMap.TryGetValue("SourceEntry", out var sourceEntryIndex) ||
            !innerMembersMap.TryGetValue("SourceId", out var sourceIdIndex) ||
            !innerMembersMap.TryGetValue("ConditionType", out var conditionTypeIndex) ||
            !innerMembersMap.TryGetValue("ConditionOperator", out var conditionOperatorIndex) ||
            !innerMembersMap.TryGetValue("ConditionTarget", out var conditionTargetIndex) ||
            !innerMembersMap.TryGetValue("NegativeCondition", out var negativeConditionIndex) ||
            !innerMembersMap.TryGetValue("ConditionIndex", out var conditionIndexIndex) ||
            !innerMembersMap.TryGetValue("ConditionParent", out var conditionParentIndex) ||
            !innerMembersMap.TryGetValue("OriginalConditionValues", out var conditionValuesIndex))
            return null;

        if (!TryExtractInt(innerMembers.Item(sourceTypeIndex).Value, out var sourceType) ||
            !TryExtractInt(innerMembers.Item(sourceGroupIndex).Value, out var sourceGroup) ||
            !TryExtractInt(innerMembers.Item(sourceEntryIndex).Value, out var sourceEntry) ||
            !TryExtractInt(innerMembers.Item(sourceIdIndex).Value, out var sourceId) ||
            !TryExtractInt(innerMembers.Item(conditionTypeIndex).Value, out var conditionType) ||
            !TryExtractInt(innerMembers.Item(conditionOperatorIndex).Value, out var conditionOperator) ||
            !TryExtractInt(innerMembers.Item(conditionTargetIndex).Value, out var conditionTarget) ||
            !TryExtractInt(innerMembers.Item(conditionIndexIndex).Value, out var conditionIndex) ||
            !TryExtractInt(innerMembers.Item(conditionParentIndex).Value, out var conditionParent))
            return null;

        bool negativeCondition = innerMembers.Item(negativeConditionIndex).Value == "true";

        var conditionValues = innerMembers.Item(conditionValuesIndex).DataMembers;
        if (conditionValues == null)
            return null;

        // -1 for [raw view]
        var conditionValuesString = Enumerable.Range(1, conditionValues.Count - 1)
            .Select(x => conditionValues.Item(x).Value.Trim('"'))
            .ToList();

        if (!conditionValuesString.Any(x => long.TryParse(x, out _)))
            return null;

        var conditionValuesLong = conditionValuesString
            .Select(long.Parse)
            .ToArray();

        var conditionTypeOrOperator = conditionIndex == 0 ? conditionType : conditionOperator;

        var condition = new AbstractCondition()
        {
            ConditionIndex = conditionIndex,
            ConditionType = conditionTypeOrOperator,
            ConditionParent = conditionParent,
            ConditionTarget = (byte)conditionTarget,
            NegativeCondition = negativeCondition ? 1 : 0,
            ConditionValue1 = conditionValuesLong[0],
            ConditionValue2 = conditionValuesLong[1],
            ConditionValue3 = conditionValuesLong[2],
            ConditionValue4 = conditionValuesLong[3],
        };

        return new ConditionBreakpointHitArgs()
        {
            Condition = condition,
            SourceEntry = sourceEntry,
            SourceType = sourceType,
            SourceGroup = sourceGroup,
            SourceId = sourceId
        };
    }

    private SmartBreakpointHitArgs? CreateSmartArgs(string? fileName, StackFrame2 stackFrame)
    {
        if (fileName == null || !fileName.Contains("SmartScript"))
            return null;

        var args = stackFrame.Arguments2[false];
        var count = args.Count;
        var argsList = Enumerable.Range(0, Math.Min(count, 10))
            .Select(x => args.Item(x + 1))
            .Select((x, index) => (x.Type, x.Name, index: index + 1))
            .ToList();
        var scriptHolderArg = argsList
            .Where(pair => pair.Type.Contains("SmartScriptHolder"))
            .Select(pair => (int?)pair.index)
            .FirstOrDefault();

        if (!scriptHolderArg.HasValue)
            return null;

        var scriptHolderExpression = (Expression2)args.Item(scriptHolderArg.Value);
        var innerMembers = scriptHolderExpression.DataMembers;
        var innerNames = Enumerable.Range(0, innerMembers.Count)
            .Select(x => innerMembers.Item(x + 1))
            .Select((x, index) => (x.Name, index))
            .ToDictionary(x => x.Name, x => x.index + 1);

        if (!innerNames.TryGetValue("entryOrGuid", out var entryOrGuidIndex) ||
            !innerNames.TryGetValue("source_type", out var sourceTypeIndex) ||
            !innerNames.TryGetValue("event_id", out var eventIdIndex)) return null;

        var entryOrGuid = innerMembers.Item(entryOrGuidIndex);
        var sourceType = innerMembers.Item(sourceTypeIndex);
        var eventId = innerMembers.Item(eventIdIndex);
        if (!entryOrGuid.IsValidValue || !sourceType.IsValidValue || !eventId.IsValidValue)
            return null;

        var entryOrGuidValue = entryOrGuid.Value;
        var sourceTypeValue = sourceType.Value;
        var eventIdValue = eventId.Value;
        if (!int.TryParse(entryOrGuidValue, out var entryOrGuidInt) ||
            !int.TryParse(eventIdValue, out var eventIdInt) ||
            !ScriptTypeNameToId.TryGetValue(sourceTypeValue, out var sourceTypeInt))
            return null;

        var isSourceArg = argsList
            .Where(pair => pair.Name == "isSource")
            .Select(pair => (int?)pair.index)
            .FirstOrDefault();

        var isSource = isSourceArg is { } isSourceIndex && args.Item(isSourceIndex).Value == "true";

        var varsArg = argsList
            .Where(pair => pair.Type.Contains("ParamList"))
            .Select(pair => (int?)pair.index)
            .FirstOrDefault();
        var strArg = argsList
            .Where(pair => pair.Type.Contains("string") && pair.Name.Contains("strParam"))
            .Select(pair => (int?)pair.index)
            .FirstOrDefault();

        List<string>? arguments = null;

        if (varsArg.HasValue)
        {
            var varsExpression = (Expression2)args.Item(varsArg.Value);
            var inner = varsExpression.DataMembers;
            if (inner != null)
            {
                // -1 for [Raw View]
                arguments = Enumerable.Range(1, inner.Count - 1)
                    .Select(index => inner.Item(index).Value)
                    .ToList();
            }
        }

        List<string>? stringArguments = null;

        if (strArg.HasValue)
        {
            var strExpression = (Expression2)args.Item(strArg.Value);
            var stringArgument = strExpression.Value;
            if (stringArgument == "0x0000000000000000")
                stringArgument = "(nullptr)";
            if (stringArgument != null)
            {
                stringArguments = new() { stringArgument };
            }
        }

        string? source = null;
        string? invoker = null;
        var localExpressions = stackFrame.Locals2[false];
        var localCount = Math.Min(localExpressions.Count, 18);
        var locals = Enumerable.Range(1, localCount)
            .Select(x => localExpressions.Item(x))
            .Select((x, index) => (x.Name, x.Type, index: index + 1))
            .ToList();
        var objIndex = locals
            .Where(x => x.Name == "obj" && x.Type.Contains("WorldObject"))
            .Select(x => (int?)x.index)
            .FirstOrDefault();
        var invokerIndex = locals
            .Where(x => x.Name == "invoker" && x.Type.Contains("WorldObject"))
            .Select(x => (int?)x.index)
            .FirstOrDefault();

        if (objIndex.HasValue)
            source = localExpressions.Item(objIndex.Value).Value;

        if (invokerIndex.HasValue)
            invoker = localExpressions.Item(invokerIndex.Value).Value;

        return new SmartBreakpointHitArgs()
        {
            EntryOrGuid = entryOrGuidInt,
            EventId = eventIdInt,
            SourceScriptType = sourceTypeInt,
            IsSource = isSource,
            Arguments = arguments?.ToArray(),
            StringArguments = stringArguments?.ToArray(),
            Source = source,
            Invoker = invoker
        };
    }

    private void OnEnterBreakMode(dbgEventReason reason, ref dbgExecutionAction executionaction)
    {
        if (dte == null)
            return;

        MessageFilter.Register();
        var currentStackFrame = (StackFrame2)dte.Debugger.CurrentStackFrame;
        var module = currentStackFrame.Module;
        var language = currentStackFrame.Language;
        var funName = currentStackFrame.FunctionName;
        var retType = currentStackFrame.ReturnType;
        var fileName = currentStackFrame.FileName;
        var lineNumber = currentStackFrame.LineNumber;
        SmartBreakpointHitArgs? breakpointHitArgs = null;
        ConditionBreakpointHitArgs? conditionBreakpointHitArgs = null;
        try
        {
            breakpointHitArgs = CreateSmartArgs(fileName, currentStackFrame);
        }
        catch (COMException) { }
        try
        {
            conditionBreakpointHitArgs = CreateConditionArgs(fileName, currentStackFrame);
        }
        catch (COMException) { }
        MessageFilter.Revoke();
        var line = GetLineSync(fileName, (int)lineNumber);

        mainThread.Dispatch(() =>
        {
            if (IsDisposed())
                return;

            BreakModeEntered?.Invoke(this, new IdeBreakpointHitEventArgs()
            {
                Language = language,
                FileName = fileName,
                LineNumber = lineNumber,
                ModuleName = module,
                FunctionName = funName,
                ReturnType = retType,
                Line = line,
                SmartBreakpointHitArgs = breakpointHitArgs,
                ConditionBreakpointHitArgs = conditionBreakpointHitArgs
            });
        });
    }

    private void OnContextChanged(Process newprocess, Program newprogram, Thread newthread, StackFrame newstackframe)
    {
        //MessageFilter.Register();
        //MessageFilter.Revoke();
    }

    private async Task InvokeOnThread(Action<DTE> action)
    {
        await Task.Run(() =>
        {
            int tries = 10;
            while (true)
            {
                try
                {
                    if (dte == null)
                        return;

                    MessageFilter.Register();
                    action(dte);
                    MessageFilter.Revoke();
                    return;
                }
                catch (COMException)
                {
                    MessageFilter.Revoke();
                    System.Threading.Thread.Sleep(100);
                    if (tries-- == 0)
                        throw new TimeoutException("Timeout: Could not invoke action on DTE");
                }
            }
        });
    }

    private async Task<T> InvokeOnThread<T>(Func<DTE, T> func)
    {
        return await Task.Run(() =>
        {
            int tries = 10;
            while (true)
            {
                try
                {
                    if (dte == null)
                        throw new ObjectDisposedException("DTE was disposed");

                    MessageFilter.Register();
                    var ret = func(dte);
                    MessageFilter.Revoke();
                    return ret;
                }
                catch (COMException)
                {
                    MessageFilter.Revoke();
                    System.Threading.Thread.Sleep(100);
                    if (tries-- == 0)
                        throw new TimeoutException("Timeout: Could not invoke action on DTE");
                }
            }
        });
    }

    public async Task<string?> GetSolutionFullPath()
    {
        if (dte == null)
            throw new ObjectDisposedException(nameof(dte));

        return await InvokeOnThread(dte => dte.Solution.FullName);
    }

    public async Task<string> GetIdeName()
    {
        if (dte == null)
            throw new ObjectDisposedException(nameof(dte));

        return await InvokeOnThread(dte =>
        {
            var versionString = dte.Version;
            var versionStringNoDot = versionString.Replace(".", "");
            if (int.TryParse(versionStringNoDot, out var versionInt) &&
                Versions.TryGetValue(versionInt, out var versionName))
            {
                versionString = versionName;
            }
            return dte.Name + " " + versionString;
        });
    }

    public async Task DebugUnpause()
    {
        if (dte == null)
            throw new ObjectDisposedException(nameof(dte));

        await InvokeOnThread(dte => dte.Debugger.Go(false));
    }

    public async Task DebugPause()
    {
        if (dte == null)
            throw new ObjectDisposedException(nameof(dte));

        await InvokeOnThread(dte => dte.Debugger.Break(false));
    }

    public async Task ActivateWindow()
    {
        if (dte == null)
            throw new ObjectDisposedException(nameof(dte));

        await InvokeOnThread(dte =>
        {
            dte.MainWindow.Activate();
            if (dte.MainWindow.WindowState == vsWindowState.vsWindowStateMinimize)
                dte.MainWindow.WindowState = vsWindowState.vsWindowStateNormal;
        });
    }

    public async Task GoToFile(string fileName, int line, bool activate)
    {
        if (dte == null)
            throw new ObjectDisposedException(nameof(dte));

        await InvokeOnThread(dte =>
        {
            var fileWindow = dte.ItemOperations.OpenFile(fileName);
            if (fileWindow == null)
                return;

            if (fileWindow.Selection is TextSelection textSelection)
                textSelection.GotoLine(line);

            if (activate)
                fileWindow.Activate();
        });
        if (activate)
            await ActivateWindow();
    }

    public async Task<dbgDebugMode> GetDebugModeAsync()
    {
        if (dte == null)
            return dbgDebugMode.dbgDesignMode;

        return await InvokeOnThread(dte => dte.Debugger.CurrentMode);
    }

    private string? GetLineSync(string fileName, int lineNumber)
    {
        if (!File.Exists(fileName))
            return null;

        return FileHelper.ExtractNthLine(fileName, lineNumber, 1000);
    }

    public async Task<string?> GetLine(string fileName, int lineNumber)
    {
        return GetLineSync(fileName, lineNumber);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.dte == null)
            return;

        var dte = this.dte;
        this.dte = null;

        await Task.Run(() =>
        {
            int tries = 50;
            while (true)
            {
                try
                {
                    MessageFilter.Register();
                    dte.Events.DebuggerEvents.OnEnterBreakMode -= OnEnterBreakMode;
                    dte.Events.DebuggerEvents.OnEnterRunMode -= OnEnterRunMode;
                    dte.Events.DebuggerEvents.OnContextChanged -= OnContextChanged;
                    dte.Events.DebuggerEvents.OnEnterDesignMode -= OnEnterDesignMode;

                    MessageFilter.Revoke();
                    return;
                }
                catch (COMException)
                {
                    MessageFilter.Revoke();
                    System.Threading.Thread.Sleep(100);
                    if (tries-- == 0)
                        throw new TimeoutException("Timeout: Could not invoke action on DTE");
                }
            }
        });
    }
}