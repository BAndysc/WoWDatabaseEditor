using System;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.ActionsOutput;

public interface IActionsOutputService
{
    IActionOutput Create(string query);
}

public interface IActionOutput
{
    int Index { get; }
    DateTime TimeStarted { get; set; }
    string Query { get; }
    
    DateTime? TimeFinished { get; set; }
    string Response { get; set; }
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