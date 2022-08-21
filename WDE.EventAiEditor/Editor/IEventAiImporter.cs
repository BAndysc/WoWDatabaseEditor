using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Editor
{
    public interface IEventAiImporter
    {
        Task Import(EventAiScript script, bool doNotTouchIfPossible, IList<IEventAiLine> lines);
    }
}