using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;

namespace WDE.PacketViewer.Solutions
{
    public class PacketDocumentSolutionItem : ISolutionItem
    {
        public PacketDocumentSolutionItem(string? file, int? version = null, bool liveStream = false)
        {
            File = file;
            CustomVersion = version;
            LiveStream = liveStream;
        }

        public string? File { get; set; }
        
        public int? CustomVersion { get; set; }

        public bool LiveStream { get; set; }

        [JsonIgnore]
        public bool IsContainer => false;

        [JsonIgnore]
        public ObservableCollection<ISolutionItem>? Items => null;

        [JsonIgnore] 
        public string? ExtraId => null;

        [JsonIgnore] 
        public bool IsExportable => false;

        public ISolutionItem Clone() => new PacketDocumentSolutionItem(File, CustomVersion);
    }
}