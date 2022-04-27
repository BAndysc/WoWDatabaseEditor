using WDE.Common.Database;
using WDE.EventScriptsEditor.ViewModels;

namespace WDE.EventScriptsEditor.Services;

public interface IEventScriptViewModelFactory
{
    EventScriptLineViewModel Factory(IEventScriptLine line);
}