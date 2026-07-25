using System;
using System.Collections.Generic;
using System.Globalization;
using SmartFormat;
using WDE.Common.Database;

namespace WDE.DbScriptsEditor.Models
{
    // Renders command description templates with SmartFormat (its built-in choose / list / conditional
    // formatters) instead of a hand-rolled expander. Templates reference parameters by their physical
    // destination column: {datalong} / {dataint2} / {x} yields the display value, and {<col>Value}
    // yields the raw numeric used by choose() conditions. {source} / {target} / {player} are actors.
    public static class DbScriptSmartFormat
    {
        private static readonly DbScriptDestination[] AllColumns =
            (DbScriptDestination[])Enum.GetValues(typeof(DbScriptDestination));

        public static string Format(string template, Dictionary<string, object> data)
        {
            try
            {
                return Smart.Format(template, data);
            }
            catch
            {
                // A malformed template must never take down the editor; show it verbatim instead.
                return template;
            }
        }

        // Seeds every column's raw value ({<col>Value}) and a plain-text fallback display ({<col>}).
        // Callers override the display of the columns that are actual parameters of the command.
        public static void SeedColumns(Dictionary<string, object> data, IDbScriptLine row)
        {
            foreach (var dest in AllColumns)
            {
                var col = DbScriptDestinations.ColumnName(dest);
                if (DbScriptDestinations.IsFloat(dest))
                {
                    var value = DbScriptDestinations.ReadFloat(row, dest);
                    data[col + "Value"] = value;
                    data[col] = value.ToString("0.###", CultureInfo.InvariantCulture);
                }
                else
                {
                    var value = DbScriptDestinations.ReadLong(row, dest);
                    data[col + "Value"] = value;
                    data[col] = value.ToString(CultureInfo.InvariantCulture);
                }
            }
        }
    }
}
