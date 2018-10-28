using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.SmartScriptEditor.Data
{
    public interface ISmartDataJsonProvider
    {
        string GetEventsJson();
        string GetActionsJson();
        string GetTargetsJson();
    }

    public interface ISmartDataProvider
    {
        IEnumerable<SmartGenericJsonData> GetEvents();
        IEnumerable<SmartGenericJsonData> GetActions();
        IEnumerable<SmartGenericJsonData> GetTargets();
    }

    public interface ISmartTypeListProvider
    {
        int? Get(SmartType type, Func<SmartGenericJsonData, bool> predicate);
    }
}
