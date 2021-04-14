using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WoWDatabaseEditorCore.Avalonia.Docking.Serialization
{
    public class SerializedDock
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public SerializedDockableType DockableType { get; set; }
        public string UniqueId { get; set; }
        public bool Horizontal { get; set; }
        public double Proportion { get; set; }
        public bool IsCollapsable { get; set; }
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