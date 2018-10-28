using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Attributes;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    public class SmartDataJsonProvider : ISmartDataJsonProvider
    {
        public string GetActionsJson()
        {
            return File.ReadAllText("SmartData/actions.json");
        }

        public string GetEventsJson()
        {
            return File.ReadAllText("SmartData/events.json");
        }

        public string GetTargetsJson()
        {
            return File.ReadAllText("SmartData/targets.json");
        }
    }
}
