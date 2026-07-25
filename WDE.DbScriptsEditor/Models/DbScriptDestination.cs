using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;

namespace WDE.DbScriptsEditor.Models
{
    // The physical data-carrying columns a parameter may serialize to.
    public enum DbScriptDestination
    {
        DataLong, DataLong2, DataLong3,
        DataInt, DataInt2, DataInt3, DataInt4,
        DataFloat, X, Y, Z, O, Speed
    }

    public enum DestinationClass
    {
        Unsigned, // datalong*
        Signed,   // dataint*
        Float     // datafloat, x, y, z, o, speed
    }

    public static class DbScriptDestinations
    {
        // Columns that belong to the structural layer and are never valid parameter destinations.
        public static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
        {
            "id", "delay", "priority", "command",
            "buddy_entry", "search_radius", "data_flags", "condition_id", "comments"
        };

        private static readonly Dictionary<string, DbScriptDestination> ByColumn = new(StringComparer.OrdinalIgnoreCase)
        {
            ["datalong"] = DbScriptDestination.DataLong,
            ["datalong2"] = DbScriptDestination.DataLong2,
            ["datalong3"] = DbScriptDestination.DataLong3,
            ["dataint"] = DbScriptDestination.DataInt,
            ["dataint2"] = DbScriptDestination.DataInt2,
            ["dataint3"] = DbScriptDestination.DataInt3,
            ["dataint4"] = DbScriptDestination.DataInt4,
            ["datafloat"] = DbScriptDestination.DataFloat,
            ["x"] = DbScriptDestination.X,
            ["y"] = DbScriptDestination.Y,
            ["z"] = DbScriptDestination.Z,
            ["o"] = DbScriptDestination.O,
            ["speed"] = DbScriptDestination.Speed,
        };

        private static readonly Dictionary<DbScriptDestination, string> ByDestination =
            ByColumn.ToDictionary(kv => kv.Value, kv => kv.Key);

        public static bool TryParse(string column, out DbScriptDestination destination) =>
            ByColumn.TryGetValue(column, out destination);

        // The physical DB column name for a destination (e.g. DataLong -> "datalong").
        public static string ColumnName(DbScriptDestination destination) => ByDestination[destination];

        public static DestinationClass Class(DbScriptDestination d) => d switch
        {
            DbScriptDestination.DataLong or DbScriptDestination.DataLong2 or DbScriptDestination.DataLong3 => DestinationClass.Unsigned,
            DbScriptDestination.DataInt or DbScriptDestination.DataInt2 or DbScriptDestination.DataInt3 or DbScriptDestination.DataInt4 => DestinationClass.Signed,
            _ => DestinationClass.Float,
        };

        public static bool IsFloat(DbScriptDestination d) => Class(d) == DestinationClass.Float;

        // Reads a destination as a long (used for the parameter value holders). Float
        // destinations are exposed separately; callers check IsFloat first.
        public static long ReadLong(IDbScriptLine row, DbScriptDestination d) => d switch
        {
            DbScriptDestination.DataLong => row.DataLong,
            DbScriptDestination.DataLong2 => row.DataLong2,
            DbScriptDestination.DataLong3 => row.DataLong3,
            DbScriptDestination.DataInt => row.DataInt,
            DbScriptDestination.DataInt2 => row.DataInt2,
            DbScriptDestination.DataInt3 => row.DataInt3,
            DbScriptDestination.DataInt4 => row.DataInt4,
            _ => 0,
        };

        public static float ReadFloat(IDbScriptLine row, DbScriptDestination d) => d switch
        {
            DbScriptDestination.DataFloat => row.DataFloat,
            DbScriptDestination.X => row.X,
            DbScriptDestination.Y => row.Y,
            DbScriptDestination.Z => row.Z,
            DbScriptDestination.O => row.O,
            DbScriptDestination.Speed => row.Speed,
            _ => 0,
        };
    }
}
