using System.Collections.Generic;
using Dock.Model.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WoWDatabaseEditorCore.Avalonia.Docking.Serialization
{
    public class SerializedDock
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public SerializedDockableType DockableType { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Alignment ToolAlignment { get; set; }
        public string UniqueId { get; set; } = "";
        public bool Horizontal { get; set; }
        public double Proportion { get; set; }
        public bool IsCollapsable { get; set; }
        public bool IsPinned { get; set; }
        public List<SerializedDock> Children { get; set; } = new();
    }

    public enum SerializedDockableType
    {
        ProportionalDock,
        DocumentDock,
        Splitter,
        ToolDock,
        Tool,
        RootDock
    }
}