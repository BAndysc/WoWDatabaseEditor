using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;

namespace WDE.DbScriptsEditor.Models
{
    // One logical parameter of a command, resolved to a physical destination column and a
    // (possibly dynamically-registered) parameter factory type key.
    public class DbScriptCommandParameter
    {
        public required string Name { get; init; }
        public required string Type { get; init; }
        public required DbScriptDestination Destination { get; init; }
        public bool Required { get; init; }
        // Pre-filled into the column when a new action is created (e.g. seat index -1 = all seats).
        public long DefaultVal { get; init; }
    }

    // A single preset condition of a variant.
    // For data_flags: OrMask bits (matched with (data_flags & mask) == mask).
    // For any other column: an exact integer value match / pre-fill.
    public class DbScriptPreset
    {
        public required string Column { get; init; }
        public required bool IsDataFlagsMask { get; init; }
        public required long Value { get; init; }
    }

    // Describes the COMMAND_ADDITIONAL (data_flags 0x8) bit for one command so it can be shown
    // as an inline two-option switch (Off = bit clear, On = bit set).
    public class DbScriptAdditionalFlag
    {
        public required string Name { get; init; }
        public required string OffLabel { get; init; }
        public required string OnLabel { get; init; }
    }

    public class DbScriptCommandVariant
    {
        public required string NameReadable { get; init; }
        public string? SearchTags { get; init; }
        public required IReadOnlyList<DbScriptPreset> Presets { get; init; }
        // null => inherit the base command's parameters
        public IReadOnlyList<DbScriptCommandParameter>? Parameters { get; init; }
        public string? Description { get; init; }
        // null => inherit the base command's source/target types (e.g. movement "Idle" overrides the
        // target to none, while "Waypoint" keeps an optional target).
        public IReadOnlyList<string>? SourceTypes { get; init; }
        public IReadOnlyList<string>? TargetTypes { get; init; }

        public bool Matches(IDbScriptLine row)
        {
            if (Presets.Count == 0)
                return false;
            foreach (var p in Presets)
            {
                if (p.IsDataFlagsMask)
                {
                    var mask = (uint)p.Value;
                    if (mask == 0 || (row.DataFlags & mask) != mask)
                        return false;
                }
                else
                {
                    if (!DbScriptDestinations.TryParse(p.Column, out var dest) ||
                        DbScriptDestinations.ReadLong(row, dest) != p.Value)
                        return false;
                }
            }
            return true;
        }
    }

    public class DbScriptCommandDefinition
    {
        public required uint Id { get; init; }
        public required string Name { get; init; }
        public required string NameReadable { get; init; }
        public string? Help { get; init; }
        public string? SearchTags { get; init; }
        public string? Group { get; init; }
        public bool Deprecated { get; init; }
        public IReadOnlyList<string> SourceTypes { get; init; } = new List<string>();
        public IReadOnlyList<string> TargetTypes { get; init; } = new List<string>();

        // Whether the command actually consumes a source / target actor. A type list that is empty
        // or only "None" means the actor is unused, so the editor won't offer it for picking.
        public bool UsesSource => SourceTypes.Count == 0 ||
                                  SourceTypes.Any(t => !string.Equals(t, "None", System.StringComparison.OrdinalIgnoreCase));
        public bool UsesTarget => TargetTypes.Any(t => !string.Equals(t, "None", System.StringComparison.OrdinalIgnoreCase));

        // Whether the command can run without a resolved source: it either ignores its source
        // entirely, or explicitly declares "None" among its source types (optional source).
        public bool AcceptsNoSource => !UsesSource ||
            SourceTypes.Any(t => string.Equals(t, "None", System.StringComparison.OrdinalIgnoreCase));

        // Kind masks of the declared source/target type lists (for actor↔command compatibility).
        private DbScriptActorKind? sourceKindMask, targetKindMask;
        public DbScriptActorKind SourceKindMask => sourceKindMask ??= DbScriptActorKinds.FromList(SourceTypes);
        public DbScriptActorKind TargetKindMask => targetKindMask ??= DbScriptActorKinds.FromList(TargetTypes);
        public DbScriptBuddyCapability Buddy { get; init; } = DbScriptBuddyCapability.Creature;
        // The command acts on "the player", resolved by the core as target-if-player-else-source
        // (GetPlayerTargetOrSourceAndLog). Enables the {player} readable token.
        public bool PlayerFromSourceOrTarget { get; init; }
        public bool SupportsAdditionalFlag { get; init; }
        // Non-null when SupportsAdditionalFlag: the labels for the inline 0x8 switch parameter.
        public DbScriptAdditionalFlag? AdditionalFlag { get; init; }
        public required IReadOnlyList<DbScriptCommandParameter> Parameters { get; init; }
        public required string Description { get; init; }
        public IReadOnlyList<DbScriptCommandVariant> Variants { get; init; } = new List<DbScriptCommandVariant>();

        // Variant-effective source/target types (a variant may narrow them, e.g. movement "Idle"
        // has no target). Falls back to the base command's types when the variant doesn't override.
        public IReadOnlyList<string> EffectiveSourceTypes(DbScriptCommandVariant? variant) =>
            variant?.SourceTypes ?? SourceTypes;
        public IReadOnlyList<string> EffectiveTargetTypes(DbScriptCommandVariant? variant) =>
            variant?.TargetTypes ?? TargetTypes;

        public bool EffectiveUsesSource(DbScriptCommandVariant? variant)
        {
            var types = EffectiveSourceTypes(variant);
            return types.Count == 0 || types.Any(t => !string.Equals(t, "None", System.StringComparison.OrdinalIgnoreCase));
        }
        public bool EffectiveUsesTarget(DbScriptCommandVariant? variant) =>
            EffectiveTargetTypes(variant).Any(t => !string.Equals(t, "None", System.StringComparison.OrdinalIgnoreCase));

        public DbScriptActorKind EffectiveSourceKindMask(DbScriptCommandVariant? variant) =>
            variant?.SourceTypes == null ? SourceKindMask : DbScriptActorKinds.FromList(variant.SourceTypes);
        public DbScriptActorKind EffectiveTargetKindMask(DbScriptCommandVariant? variant) =>
            variant?.TargetTypes == null ? TargetKindMask : DbScriptActorKinds.FromList(variant.TargetTypes);

        // Resolves the effective definition (parameters + description) for a concrete row,
        // by picking the most-specific matching variant (already sorted). Falls back to base.
        public (IReadOnlyList<DbScriptCommandParameter> parameters, string description, DbScriptCommandVariant? variant) Resolve(IDbScriptLine row)
        {
            foreach (var variant in Variants)
            {
                if (variant.Matches(row))
                    return (variant.Parameters ?? Parameters, variant.Description ?? Description, variant);
            }
            return (Parameters, Description, null);
        }
    }
}
