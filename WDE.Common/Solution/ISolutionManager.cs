using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [UniqueProvider]
    public interface ISolutionManager
    {
        void Initialize();
        ObservableCollection<ISolutionItem> Items { get; }
    }
}
