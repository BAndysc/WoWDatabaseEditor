using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;

namespace WDE.PacketViewer.Solutions
{
    public class PacketDocumentSolutionItem : ISolutionItem
    {
        public PacketDocumentSolutionItem(string file)
        {
            File = file;
        }

        public string File { get; set; }
        
        [JsonIgnore]
        public bool IsContainer => false;

        [JsonIgnore]
        public ObservableCollection<ISolutionItem>? Items => null;

        [JsonIgnore] 
        public string? ExtraId => null;

        [JsonIgnore] 
        public bool IsExportable => false;
    }
}