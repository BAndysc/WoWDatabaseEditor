using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;

namespace WDE.Common.Events
{
    public class EventRequestGenerateSqlArgs
    {
        public ISolutionItem Item { get; set; }
        public string Sql { get; set; }
    }

    public class EventRequestGenerateSql : PubSubEvent<EventRequestGenerateSqlArgs>
    {
    }
}
