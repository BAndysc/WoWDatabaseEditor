using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor
{
    public abstract class EventAiIconBaseProvider<T> : ISolutionItemIconProvider<T> where T : IEventAiSolutionItem
    {
        public virtual ImageUri GetIcon(T item) => new ImageUri("Icons/document_creature.png");
    }
}