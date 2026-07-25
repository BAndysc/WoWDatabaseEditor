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

            Readable = RenderReadable(description, parameters, typeInfo, factory, definition);
            UnusedColumns = ComputeUnusedColumns(parameters, variant);
        }

        private string RenderReadable(string description, IReadOnlyList<DbScriptCommandParameter> parameters,
            DbScriptTypeInfo typeInfo, IParameterFactory factory, DbScriptCommandDefinition definition)
        {
            var hasActorColon = DbScriptReadableCase.StartsWithActorToken(description);
            var decoded = DbScriptFlagsCodec.Decode(Raw.DataFlags, Raw.BuddyEntry, Raw.SearchRadius, definition.Buddy);
            var (source, target) = BuddyDescriptorFormatter.ResolveActors(Raw, typeInfo, definition.Buddy);

            // SmartFormat data object: raw column values + a plain formatted display per parameter,
            // keyed by destination column. Same template as the editable renderer, no clickable links.
            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            DbScriptSmartFormat.SeedColumns(data, Raw);
            data["source"] = source;
            data["target"] = target;
            // {player} / {creature}: the actual actor the core resolves from source/target.
            if (description.Contains("{player}", StringComparison.Ordinal))
                data["player"] = BuddyDescriptorFormatter.ResolveActorOfKind(decoded, typeInfo, DbScriptActorKind.Player, preferTarget: true).label;
            if (description.Contains("{creature}", StringComparison.Ordinal))
                data["creature"] = BuddyDescriptorFormatter.ResolveActorOfKind(decoded, typeInfo, DbScriptActorKind.Creature, preferTarget: false).label;

            foreach (var p in parameters)
            {
                var col = DbScriptDestinations.ColumnName(p.Destination);
                data[col] = DbScriptDestinations.IsFloat(p.Destination)
                    ? DbScriptDestinations.ReadFloat(Raw, p.Destination).ToString("0.###", CultureInfo.InvariantCulture)
                    : SafeToString(factory.Factory(p.Type), DbScriptDestinations.ReadLong(Raw, p.Destination));
            }

            var rendered = DbScriptSmartFormat.Format(description, data);

            // A buddy that occupies neither slot (TERMINATE_SCRIPT's buddyFound condition, or an
            // existence gate on other commands) is invisible in the source/target sentence — show it.
            if (decoded.Buddy.Provided && !decoded.Direction.UsesBuddy)
            {
                var label = BuddyDescriptorFormatter.Format(decoded.Buddy);
                rendered += Raw.Command == DbScriptInspections.TerminateScriptCommandId
                    ? $" · condition: {label}"
                    : $" · only if {label} present";
            }

            return DbScriptReadableCase.SentenceCase(rendered, hasActorColon);
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
