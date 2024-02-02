using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.Debugger.Services.Logs.LogExpressions;
using WDE.Debugger.Services.Logs.LogExpressions.Antlr;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.Debugger.ViewModels.Logs;

[AutoRegister]
[SingleInstance]
internal partial class DebuggerLogsToolViewModel : ObservableBase, ITool
{
    private readonly IParameterFactory parameterFactory;
    private readonly ILogExpressionService logExpressionService;
    [Notify] private bool visibility;
    [Notify] private bool isSelected;
    public string Title => "Debugger logs";
    public string UniqueId => "debugger_logs_tool";
    public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Bottom;
    public bool OpenOnStart => false;

    [Notify] private INodeType? selectedNode;

    private ObservableCollection<IParentType> roots = new();
    public FlatTreeList<IParentType, IChildType> FlatTree { get; }

    public DelegateCommand ClearCommand { get; }

    public ICommand CopySelectedNodeCommand { get; }

    public DebuggerLogsToolViewModel(Services.DebuggerLogService service,
        IParameterFactory parameterFactory,
        ILogExpressionService logExpressionService,
        IClipboardService clipboardService)
    {
        this.parameterFactory = parameterFactory;
        this.logExpressionService = logExpressionService;
        service.OnLog += OnLog;
        FlatTree = new(roots);

        ClearCommand = new(() =>
        {
            roots.RemoveAll();
        });

        CopySelectedNodeCommand = new DelegateCommand(() =>
        {
            if (selectedNode is DebuggerLogObjectViewModel logNode)
            {
                clipboardService.SetText(logNode.Token.ToString());
            }
            else if (selectedNode is DebuggerLogValueViewModel simpleNode)
            {
                clipboardService.SetText(simpleNode.Token.ToString());
            }
            else if (selectedNode is DebuggerLogTextViewModel textLog)
            {
                clipboardService.SetText(textLog.Token.ToString());
            }
        });
    }

    private IParameter<long>? GetParameterForKey(string key)
    {
        switch (key)
        {
            case "flags":
                return parameterFactory.Factory("UnitFlagsParameter");
            case "flags2":
                return parameterFactory.Factory("UnitFlags2Parameter");
            case "flags3":
                return parameterFactory.Factory("UnitFlags3Parameter");
            case "displayid":
                return parameterFactory.Factory("CreatureModelDataParameter");
            case "bytes0":
                return parameterFactory.Factory("UnitBytes0Parameter");
            case "bytes1":
                return parameterFactory.Factory("UnitBytes1Parameter");
            case "bytes2":
                return parameterFactory.Factory("UnitBytes2Parameter");
            case "npcflags":
                return parameterFactory.Factory("NpcFlagParameter");
            case "npcflags2":
                return parameterFactory.Factory("NpcFlag2Parameter");
        }

        return null;
    }

    private INodeType CreateLogViewModel(string parentKey, JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var node = new DebuggerLogObjectViewModel(token, parentKey, DebuggerLogTokenType.Object);
            foreach (var child in (JObject)token)
            {
                var key = child.Key;
                if (child.Value == null || child.Value.Type == JTokenType.Null)
                {
                    var valueNode = new DebuggerLogValueViewModel(JValue.CreateNull(), key, "(null)", null);
                    node.Add(valueNode);
                }
                else
                {
                    var valueNode = CreateLogViewModel(key, child.Value);
                    node.Add(valueNode);
                }
            }
            node.UpdateCollapsedText();
            return node;
        }
        else if (token.Type == JTokenType.Array)
        {
            var node = new DebuggerLogObjectViewModel(token, parentKey, DebuggerLogTokenType.Array);
            int index = 0;
            foreach (var childToken in (JArray)token)
            {
                var childNode = CreateLogViewModel(index.ToString(), childToken);
                node.Add(childNode);
                index++;
            }
            node.UpdateCollapsedText();

            return node;
        }
        else
        {
            if (token.Type == JTokenType.Integer)
            {
                var value = token.Value<long>();
                var parameter = GetParameterForKey(parentKey);
                return new DebuggerLogValueViewModel(token, parentKey, value.ToString(), parameter?.ToString(value, ToStringOptions.WithoutNumber));
            }
            else if (token.Type == JTokenType.Float)
            {
                return new DebuggerLogValueViewModel(token, parentKey, token.Value<float>().ToString(CultureInfo.InvariantCulture), null);
            }
            else if (token.Type == JTokenType.String)
            {
                return new DebuggerLogValueViewModel(token, parentKey, token.Value<string>() ?? "(null)", null);
            }
            else
            {
                return new DebuggerLogValueViewModel(token, parentKey, token.ToString(), null);
            }
        }
    }

    private void OnLog((string title, string? logFormat, string? logCondition, JObject log) data)
    {
        JToken logToken = data.log;

        if (!string.IsNullOrEmpty(data.logCondition))
        {
            try
            {
                var condition = logExpressionService.Parse(data.log, data.logCondition);
                if (condition.Type != JTokenType.Boolean)
                    throw new LogParseException("Condition must return a boolean value");
                if (condition.Value<bool>() == false)
                    return;
            }
            catch (LogParseException e)
            {
                logToken = "ERROR: " + e.Message;
            }
        }

        if (!string.IsNullOrEmpty(data.logFormat))
        {
            try
            {
                logToken = logExpressionService.Parse(data.log, data.logFormat);
            }
            catch (LogParseException e)
            {
                logToken = "ERROR: " + e.Message;
            }
        }

        if (logToken is JValue value)
        {
            roots.Add(new DebuggerLogTextViewModel(value));
        }
        else
        {
            var now = DateTime.Now.ToString("HH:mm:ss");
            roots.Add((DebuggerLogObjectViewModel)CreateLogViewModel($"[{now}] {data.title}", logToken));
        }
    }
}