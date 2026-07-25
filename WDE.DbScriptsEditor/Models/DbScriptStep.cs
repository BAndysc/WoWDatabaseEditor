using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using WDE.Common.Database;
using WDE.Common.Parameters;

namespace WDE.DbScriptsEditor.Models
{
    // Read-only representation of one dbscript row. Keeps the raw column values so an
    // untouched script roundtrips byte-identically; overlays the resolved command meaning.
    public class DbScriptStep
    {
        private static readonly Regex TokenRegex = new(@"\{([A-Za-z0-9 _]+?)\}", RegexOptions.Compiled);

        private static readonly DbScriptDestination[] DataColumns =
        {
            DbScriptDestination.DataLong, DbScriptDestination.DataLong2, DbScriptDestination.DataLong3,
            DbScriptDestination.DataInt, DbScriptDestination.DataInt2, DbScriptDestination.DataInt3, DbScriptDestination.DataInt4,
            DbScriptDestination.DataFloat, DbScriptDestination.X, DbScriptDestination.Y, DbScriptDestination.Z,
            DbScriptDestination.O, DbScriptDestination.Speed
        };

        public IDbScriptLine Raw { get; }
        public uint DelayMs => Raw.Delay;
        public uint Priority => Raw.Priority;
        public uint CommandId => Raw.Command;
        public uint ConditionId => Raw.ConditionId;
        public string? Comment => Raw.Comments;

        public string CommandName { get; }
        public string? VariantName { get; }
        public string Readable { get; }
        public IReadOnlyList<string> UnusedColumns { get; }
        public bool HasUnusedColumnWarning => UnusedColumns.Count > 0;

        public DbScriptStep(IDbScriptLine row, DbScriptCommandDefinition? definition, DbScriptTypeInfo typeInfo, IParameterFactory factory)
        {
            Raw = row;

            if (definition == null)
            {
                CommandName = $"Unknown command {row.Command}";
                Readable = $"Unknown command {row.Command}";
                UnusedColumns = Array.Empty<string>();
                return;
            }

            var (parameters, description, variant) = definition.Resolve(row);
            CommandName = definition.NameReadable;
            VariantName = variant?.NameReadable;

            Readable = RenderReadable(description, parameters, typeInfo, factory);
            UnusedColumns = ComputeUnusedColumns(parameters, variant);
        }

        private string RenderReadable(string description, IReadOnlyList<DbScriptCommandParameter> parameters,
            DbScriptTypeInfo typeInfo, IParameterFactory factory)
        {
            var (source, target) = DbScriptActorResolver.Resolve(Raw, typeInfo);
            var friendly = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["source"] = source,
                ["target"] = target,
            };

            // per-parameter friendly value + whether the raw column is non-default
            var nonDefault = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in parameters)
            {
                if (DbScriptDestinations.IsFloat(p.Destination))
                {
                    var value = DbScriptDestinations.ReadFloat(Raw, p.Destination);
                    friendly[p.Name] = value.ToString("0.###", CultureInfo.InvariantCulture);
                    if (Math.Abs(value - p.DefaultVal) > float.Epsilon)
                        nonDefault.Add(p.Name);
                }
                else
                {
                    var value = DbScriptDestinations.ReadLong(Raw, p.Destination);
                    friendly[p.Name] = SafeToString(factory.Factory(p.Type), value);
                    if (value != p.DefaultVal)
                        nonDefault.Add(p.Name);
                }
            }

            // referenced is computed from the un-expanded description on purpose: a parameter
            // mentioned only inside a non-chosen choose branch still counts as "mentioned".
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in TokenRegex.Matches(description))
                referenced.Add(m.Groups[1].Value.Trim());

            description = DbScriptDescriptionChoose.Expand(description, name =>
            {
                var p = parameters.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (p == null)
                    return null;
                return DbScriptDestinations.IsFloat(p.Destination)
                    ? DbScriptDestinations.ReadFloat(Raw, p.Destination).ToString("0.###", CultureInfo.InvariantCulture)
                    : DbScriptDestinations.ReadLong(Raw, p.Destination).ToString(CultureInfo.InvariantCulture);
            });

            var rendered = TokenRegex.Replace(description, m =>
            {
                var token = m.Groups[1].Value.Trim();
                return friendly.TryGetValue(token, out var value) ? value : m.Value;
            });

            // Enrich: append any non-default parameter the sentence didn't already mention,
            // so nothing meaningful is hidden (e.g. TERMINATE_SCRIPT's search creature/distance).
            var extras = parameters
                .Where(p => nonDefault.Contains(p.Name) && !referenced.Contains(p.Name))
                .Select(p => $"{p.Name}: {friendly[p.Name]}")
                .ToList();

            return extras.Count > 0 ? $"{rendered} · {string.Join(", ", extras)}" : rendered;
        }

        private static string SafeToString(IParameter<long> parameter, long value)
        {
            try
            {
                return parameter.ToString(value);
            }
            catch
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        private List<string> ComputeUnusedColumns(IReadOnlyList<DbScriptCommandParameter> parameters, DbScriptCommandVariant? variant)
        {
            var used = new HashSet<DbScriptDestination>(parameters.Select(p => p.Destination));
            if (variant != null)
            {
                foreach (var preset in variant.Presets)
                {
                    if (!preset.IsDataFlagsMask && DbScriptDestinations.TryParse(preset.Column, out var dest))
                        used.Add(dest);
                }
            }

            var result = new List<string>();
            foreach (var col in DataColumns)
            {
                if (used.Contains(col))
                    continue;
                var nonZero = DbScriptDestinations.IsFloat(col)
                    ? Math.Abs(DbScriptDestinations.ReadFloat(Raw, col)) > float.Epsilon
                    : DbScriptDestinations.ReadLong(Raw, col) != 0;
                if (nonZero)
                    result.Add(col.ToString());
            }
            return result;
        }
    }
}
