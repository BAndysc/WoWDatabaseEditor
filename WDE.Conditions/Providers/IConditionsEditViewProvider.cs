using System.Collections.ObjectModel;
using WDE.Module.Attributes;
using WDE.Conditions.Model;

namespace WDE.Conditions.Providers
{
    public interface IConditionsEditViewProvider
    {
        void OpenWindow(ObservableCollection<Condition> conditions);
    }
}
