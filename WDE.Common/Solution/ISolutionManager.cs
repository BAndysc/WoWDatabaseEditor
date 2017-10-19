using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common
{
    public interface ISolutionManager
    {
        void Initialize();
        ObservableCollection<ISolutionItem> Items { get; }
    }
}
