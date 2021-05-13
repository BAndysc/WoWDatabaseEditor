using Prism.Events;
using WDE.Common.Managers;

namespace WDE.Common.Events
{
    public class EventActiveDocumentChanged : PubSubEvent<IDocument?>
    {
    }
}