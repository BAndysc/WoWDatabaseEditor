using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;

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

        void SetUnity(IUnityContainer unity);

        [JsonIgnore]
        bool IsExportable { get; }
        [JsonIgnore]
        string ExportSql { get; }
    }
}
