using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.EventAiEditor.Editor
{
    public enum EventAiPropertyType
    {
        Event,
        Action1,
        Action2,
        Action3
    }
    
    public interface IEventAiDatabaseProvider
    {
        Task<IEnumerable<IEventAiLine>> GetScriptFor(int entryOrGuid);
        Task<IList<IEventAiLine>> FindEventAiLinesBy(IEnumerable<(EventAiPropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions);
    }
}