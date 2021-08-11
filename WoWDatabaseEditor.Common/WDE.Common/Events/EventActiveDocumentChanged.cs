using Prism.Events;
using WDE.Common.Managers;

namespace WDE.Common.Events
{
    public class EventActiveDocumentChanged : PubSubEvent<IDocument?>
    {
    }
    public class EventActiveUndoRedoDocumentChanged : PubSubEvent<IUndoRedoWindow?>
    {
    }
}