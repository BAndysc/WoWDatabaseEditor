using System.Collections.ObjectModel;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [UniqueProvider]
    public interface ISolutionManager
    {
        ObservableCollection<ISolutionItem> Items { get; }
        void Initialize();
    }
}