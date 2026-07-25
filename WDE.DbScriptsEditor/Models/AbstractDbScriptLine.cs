using WDE.Common.Database;

namespace WDE.DbScriptsEditor.Models
{
    // A plain mutable IDbScriptLine, used to snapshot an editable step for rendering/export.
    public class AbstractDbScriptLine : IDbScriptLine
    {
        public uint Id { get; set; }
        public uint Delay { get; set; }
        public uint Priority { get; set; }
        public uint Command { get; set; }
        public uint DataLong { get; set; }
        public uint DataLong2 { get; set; }
        public uint DataLong3 { get; set; }
        public uint BuddyEntry { get; set; }
        public uint SearchRadius { get; set; }
        public uint DataFlags { get; set; }
        public int DataInt { get; set; }
        public int DataInt2 { get; set; }
        public int DataInt3 { get; set; }
        public int DataInt4 { get; set; }
        public float DataFloat { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float O { get; set; }
        public float Speed { get; set; }
        public uint ConditionId { get; set; }
        public string? Comments { get; set; }

        // Copies any line into a plain mutable snapshot (used for clipboard/export).
        public static AbstractDbScriptLine From(IDbScriptLine l) => new()
        {
            Id = l.Id, Delay = l.Delay, Priority = l.Priority, Command = l.Command,
            DataLong = l.DataLong, DataLong2 = l.DataLong2, DataLong3 = l.DataLong3,
            BuddyEntry = l.BuddyEntry, SearchRadius = l.SearchRadius, DataFlags = l.DataFlags,
            DataInt = l.DataInt, DataInt2 = l.DataInt2, DataInt3 = l.DataInt3, DataInt4 = l.DataInt4,
            DataFloat = l.DataFloat, X = l.X, Y = l.Y, Z = l.Z, O = l.O, Speed = l.Speed,
            ConditionId = l.ConditionId, Comments = l.Comments,
        };
    }
}
