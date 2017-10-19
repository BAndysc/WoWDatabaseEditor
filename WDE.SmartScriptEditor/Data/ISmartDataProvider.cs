using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.SmartScriptEditor.Data
{
    public interface ISmartDataProvider
    {
        string GetEventsJson();
        string GetActionsJson();
        string GetTargetsJson();
    }

    public interface ISmartTypeListProvider
    {
        int? Get(SmartType type, Func<SmartGenericJsonData, bool> predicate);
    }
}
