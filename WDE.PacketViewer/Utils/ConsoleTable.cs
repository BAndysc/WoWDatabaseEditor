using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleTables
{
    public class ConsoleTable
    {
        public IList<object> Columns { get; set; }
        public IList<object?[]?> Rows { get; protected set; }
        public Dictionary<int, List<(int col, Alignment span)>> ColAlignments { get; private set; }
        public Dictionary<int, List<(int col, int align)>> ColSpans { get; private set; }
        public Dictionary<int, List<int>> DisableTopBorder { get; private set; }

        public ConsoleTableOptions Options { get; protected set; }
        public Type[]? ColumnTypes { get; private set; }

        public static HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(int),  typeof(double),  typeof(decimal),
            typeof(long), typeof(short),   typeof(sbyte),
            typeof(byte), typeof(ulong),   typeof(ushort),
            typeof(uint), typeof(float)
        };

        public ConsoleTable(params string[] columns)
            : this(new ConsoleTableOptions { Columns = new List<string>(columns) })
        {
        }

        public ConsoleTable(ConsoleTableOptions? options)
        {
            Options = options ?? throw new ArgumentNullException("options");
            Rows = new List<object?[]?>();
            ColSpans = new();
            DisableTopBorder = new();
            ColAlignments = new();
            Columns = new List<object>(options.Columns);
        }

        public ConsoleTable AddColumn(IEnumerable<string> names)
        {
            foreach (var name in names)
                Columns.Add(name);
            return this;
        }
        
        public void AddDoubleRowDivider()
        {
            Rows.Add(null);
        }

        public void DisableNextRowCellTopBorder(int column)
        {
            int nextRowId = Rows.Count;
            if (!DisableTopBorder.ContainsKey(nextRowId))
                DisableTopBorder[nextRowId] = new List<int>();
            DisableTopBorder[nextRowId].Add(column);
        }
        
        public void SetCellAlignment(int row, int col, Alignment alignment)
        {
            if (!ColAlignments.ContainsKey(row))
                ColAlignments[row] = new List<(int col, Alignment span)>();
            ColAlignments[row].Add((col, alignment));
        }
        
        public void SetCellSpan(int row, int col, int span)
        {
            if (!ColSpans.ContainsKey(row))
                ColSpans[row] = new List<(int col, int span)>();
            ColSpans[row].Add((col, span));
        }
        
        public ConsoleTable AddRow(params object?[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (!Columns.Any())
                throw new Exception("Please set the columns first");

            if (Columns.Count != values.Length)
                throw new Exception(
                    $"The number columns in the row ({Columns.Count}) does not match the values ({values.Length})");

            Rows.Add(values);
            return this;
        }

        public ConsoleTable Configure(Action<ConsoleTableOptions> action)
        {
            action(Options);
            return this;
        }

        public static ConsoleTable From<T>(IEnumerable<T> values)
        {
            var table = new ConsoleTable
            {
                ColumnTypes = GetColumnsType<T>().ToArray()
            };

            var columns = GetColumns<T>();

            table.AddColumn(columns);

            foreach (
                var propertyValues
                in values.Select(value => columns.Select(column => GetColumnValue<T>(value!, column)))
            ) table.AddRow(propertyValues.ToArray());

            return table;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            // find the longest column by searching each row
            var columnLengths = ColumnLengths();

            // set right alinment if is a number
            var columnAlignment = Enumerable.Range(0, Columns.Count)
                .Select(GetNumberAlignment)
                .ToList();

            // create the string format with padding
            var format = Enumerable.Range(0, Columns.Count)
                .Select(i => " | {" + i + "," + columnAlignment[i] + columnLengths[i] + "}")
                .Aggregate((s, a) => s + a) + " |";

            // find the longest formatted line
            var maxRowLength = Math.Max(0, Rows.Any(r => r != null) ? Rows.Where(r => r != null).Max(row => string.Format(format, row!).Length) : 0);
            var columnHeaders = string.Format(format, Columns.ToArray());

            // longest line is greater of formatted columnHeader and longest row
            var longestLine = Math.Max(maxRowLength, columnHeaders.Length);

            // add each row
            var results = Rows.Where(r => r != null).Select(row => string.Format(format, row!)).ToList();

            // create the divider
            var divider = " " + string.Join("", Enumerable.Repeat("-", longestLine - 1)) + " ";

            builder.AppendLine(divider);
            builder.AppendLine(columnHeaders);

            foreach (var row in results)
            {
                builder.AppendLine(divider);
                builder.AppendLine(row);
            }

            builder.AppendLine(divider);

            if (Options.EnableCount)
            {
                builder.AppendLine("");
                builder.AppendFormat(" Count: {0}", Rows.Count);
            }

            return builder.ToString();
        }

        public string ToMarkDownString()
        {
            return ToMarkDownString('|');
        }

        private string ToMarkDownString(char delimiter)
        {
            var builder = new StringBuilder();

            // find the longest column by searching each row
            var columnLengths = ColumnLengths();

            // create the string format with padding
            var format = Format(columnLengths, delimiter);

            // find the longest formatted line
            var columnHeaders = string.Format(format, Columns.ToArray());

            // add each row
            var results = Rows.Where(r => r != null).Select(row => string.Format(format, row!)).ToList();

            // create the divider
            var divider = Regex.Replace(columnHeaders, @"[^|]", "-");

            builder.AppendLine(columnHeaders);
            builder.AppendLine(divider);
            results.ForEach(row => builder.AppendLine(row));

            return builder.ToString();
        }

        public string ToMinimalString()
        {
            return ToMarkDownString(char.MinValue);
        }

        public string ToStringAlternative()
        {
            var builder = new StringBuilder();

            // find the longest column by searching each row
            var columnLengths = ColumnLengths();

            // create the string format with padding
            var format = Format(columnLengths);

            // find the longest formatted line
            var columnHeaders = string.Format(format, Columns.ToArray());

            // add each row
            var results = Rows.Select((row, rowIndex) =>
            {
                if (row == null)
                    return null;
                if (ColSpans.TryGetValue(rowIndex, out var span))
                {
                    var fmt = Format(columnLengths, span, '|', out var lengths);
                    if (ColAlignments.TryGetValue(rowIndex, out var alignments))
                    {
                        foreach (var pair in alignments)
                        {
                            var str = row[pair.col]!.ToString() ?? "(null)";
                            var len = lengths.FirstOrDefault(p => p.ith == pair.col).len;
                            var leftPad = (len - str.Length) / 2;
                            row[pair.col] = str.PadLeft(leftPad + str.Length, ' ').PadRight(len, ' ');
                        }
                    }
                    return string.Format(fmt, row);
                }
                return string.Format(format, row);
            }).ToList();

            // create the divider
            var divider = Regex.Replace(columnHeaders.Replace(" @", "@@").Replace("@ ", "@@"), @"[^|@]", "-").Replace('@', ' ');
            var dividerPlus = divider.Replace('|', '+');
            var doubleDivider = dividerPlus.Replace('-', '=');

            builder.AppendLine(dividerPlus);
            builder.AppendLine(columnHeaders.Replace('@', ' '));

            bool nextIsDouble = false;
            int rowId = 0;
            foreach (var row in results)
            {
                rowId++;
                if (row == null)
                {
                    nextIsDouble = true;
                    continue;
                }

                if (DisableTopBorder.TryGetValue(rowId - 1, out var customBorder))
                {
                    builder.Append('|');
                    int k = 0;
                    for (var ith = 0; ith < columnLengths.Count; ith++)
                    {
                        var len = columnLengths[ith];
                        char filler = '-';
                        if (k < customBorder.Count && customBorder[k] == ith)
                        {
                            filler = ' ';
                            k++;
                        }
                        for (int m = 0; m < len + 2; m++)
                            builder.Append(filler);
                        builder.Append(ith == columnLengths.Count - 1 ? '|' : '+');
                    }

                    builder.AppendLine();
                }
                else
                    builder.AppendLine(nextIsDouble ? doubleDivider : dividerPlus);
                builder.AppendLine(row);
                nextIsDouble = false;
            }
            builder.AppendLine(dividerPlus);

            return builder.ToString();
        }

        private string Format(List<int> columnLengths, char delimiter = '|')
        {
            // set right alinment if is a number
            var columnAlignment = Enumerable.Range(0, Columns.Count)
                .Select(GetNumberAlignment)
                .ToList();

            var delimiterStr = delimiter == char.MinValue ? string.Empty : delimiter.ToString();
            var format = (Enumerable.Range(0, Columns.Count)
                .Select(i => " " + delimiterStr + " {" + i + "," + columnAlignment[i] + columnLengths[i] + "}")
                .Aggregate((s, a) => s + a) + " " + delimiterStr).Trim();
            return format;
        }

        private string Format(List<int> columnLengths, List<(int col, int span)> spans, char delimiter, out List<(int len, int ith)> mergedLengths)
        { 
            mergedLengths = new();
            int j = 0;
            for (int i = 0; i < columnLengths.Count; ++i)
            {
                if (j < spans.Count && i == spans[j].col)
                {
                    int mergedLength = (spans[j].span - 1) * 3; // for delims
                    for (int k = 0; k < spans[j].span; ++k)
                        mergedLength += columnLengths[i + k];
                    mergedLengths.Add((mergedLength, i));
                    i += spans[j].span - 1;
                    ++j;
                }
                else
                {
                    mergedLengths.Add((columnLengths[i], i));
                }
            }
            
            // set right alinment if is a number
            var columnAlignment = Enumerable.Range(0, columnLengths.Count)
                .Select(GetNumberAlignment)
                .ToList();

            var delimiterStr = delimiter == char.MinValue ? string.Empty : delimiter.ToString();
            var format = (mergedLengths
                .Select(i => " " + delimiterStr + " {" + i.ith + "," + columnAlignment[i.ith] + i.len + "}")
                .Aggregate((s, a) => s + a) + " " + delimiterStr).Trim();
            return format;
        }

        private string GetNumberAlignment(int i)
        {
            return Options.NumberAlignment == Alignment.Right
                    && ColumnTypes != null
                    && NumericTypes.Contains(ColumnTypes[i])
                ? ""
                : "-";
        }

        private List<int> ColumnLengths()
        {
            var columnLengths = Columns
                .Select((t, i) => Rows.Where(r => r != null).Select(x => x![i])
                    .Union(new[] { Columns[i] })
                    .Where(x => x != null)
                    .Select(x => x!.ToString()!.Length).Max())
                .ToList();
            return columnLengths;
        }

        public void Write(Format format = ConsoleTables.Format.Default)
        {
            switch (format)
            {
                case ConsoleTables.Format.Default:
                    Options.OutputTo.WriteLine(ToString());
                    break;
                case ConsoleTables.Format.MarkDown:
                    Options.OutputTo.WriteLine(ToMarkDownString());
                    break;
                case ConsoleTables.Format.Alternative:
                    Options.OutputTo.WriteLine(ToStringAlternative());
                    break;
                case ConsoleTables.Format.Minimal:
                    Options.OutputTo.WriteLine(ToMinimalString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private static IEnumerable<string> GetColumns<T>()
        {
            return typeof(T).GetProperties().Select(x => x.Name).ToArray();
        }

        private static object? GetColumnValue<T>(object target, string column)
        {
            return typeof(T).GetProperty(column)!.GetValue(target, null);
        }

        private static IEnumerable<Type> GetColumnsType<T>()
        {
            return typeof(T).GetProperties().Select(x => x.PropertyType).ToArray();
        }
    }

    public class ConsoleTableOptions
    {
        public IEnumerable<string> Columns { get; set; } = new List<string>();
        public bool EnableCount { get; set; } = true;

        /// <summary>
        /// Enable only from a list of objects
        /// </summary>
        public Alignment NumberAlignment { get; set; } = Alignment.Left;

        /// <summary>
        /// The <see cref="TextWriter"/> to write to. Defaults to <see cref="Console.Out"/>.
        /// </summary>
        public TextWriter OutputTo { get; set; } = Console.Out;
    }

    public enum Format
    {
        Default = 0,
        MarkDown = 1,
        Alternative = 2,
        Minimal = 3
    }

    public enum Alignment
    {
        Left,
        Center,
        Right
    }
}