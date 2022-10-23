using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Profiles;

namespace WDE.Profiles.Services;

public interface IInterEditorCommunication
{
    Task<IList<RunningProfile>> GetRunning();
    void RequestActivate(RunningProfile instance);
    void RequestOpen(RunningProfile instance, ISmartScriptProjectItem projectItem);

    void Open();
    void Close();
    
    IObservable<ISmartScriptProjectItem> OpenRequest { get; }
    IObservable<Unit> BringToFrontRequest { get; }
}