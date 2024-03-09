using System.Threading.Tasks;

namespace WDE.EventScriptsEditor.EventScriptData;

public interface IEventScriptDataJsonProvider
{
    Task<string> GetJson();
}