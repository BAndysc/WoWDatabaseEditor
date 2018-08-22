using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using Newtonsoft.Json;
using Prism.Ioc;

namespace WDE.Common
{
    public interface ISolutionItem
    {
        bool IsContainer { get; }
        ObservableCollection<ISolutionItem> Items { get; }

        [JsonIgnore]
        string Name { get; }
        [JsonIgnore]
        string ExtraId { get; }
        
        [JsonIgnore]
        bool IsExportable { get; }
        [JsonIgnore]
        string ExportSql { get; }
    }
}
