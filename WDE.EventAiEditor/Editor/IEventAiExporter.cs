using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.EventAiEditor.Models;
using WDE.SqlQueryGenerator;

namespace WDE.EventAiEditor.Editor.UserControls
{
    public interface IEventAiExporter
    {
        IEventAiLine[] ToDatabaseCompatibleEventAi(EventAiScript script);
        Task<IQuery> GenerateSql(IEventAiSolutionItem item, EventAiScript script);
    }
}