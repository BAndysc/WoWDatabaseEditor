using System;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.ActionsOutput;

public interface IActionsOutputService : ITool
{
    IActionOutput Create(string query);
}

public interface IActionOutput
{
    int Index { get; }
    DateTime TimeStarted { get; set; }
    string Query { get; }
    string OriginalQuery { get; }
    
    DateTime? TimeFinished { get; set; }
    string Response { get; set; }
    Exception? Exception { get; set; }
    ActionStatus Status { get; set; }
}

public enum ActionStatus
{
    NotStarted,
    Started,
    Success,
    Error,
    Canceled
}