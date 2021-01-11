using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using Newtonsoft.Json;
using Prism.Ioc;
using WDE.Common.Solution;

namespace WDE.Common
{
    public interface ISolutionItem
    {
        bool IsContainer { get; }
        ObservableCollection<ISolutionItem> Items { get; }
        
        [JsonIgnore]
        string ExtraId { get; }
        
        [JsonIgnore]
        bool IsExportable { get; }
    }
}
