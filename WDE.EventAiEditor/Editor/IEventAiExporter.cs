using WDE.Common.Database;
using WDE.EventAiEditor.Models;
using WDE.SqlQueryGenerator;

namespace WDE.EventAiEditor.Editor.UserControls
{
    public interface IEventAiExporter
    {
        IEventAiLine[] ToDatabaseCompatibleEventAi(EventAiScript script);
        IQuery GenerateSql(IEventAiSolutionItem item, EventAiScript script);
    }
}