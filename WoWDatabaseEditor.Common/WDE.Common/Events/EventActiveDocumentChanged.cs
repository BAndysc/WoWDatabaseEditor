using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Managers;

namespace WDE.Common.Events
{
    public class EventActiveDocumentChanged : PubSubEvent<IDocument>
    {
    }
}
