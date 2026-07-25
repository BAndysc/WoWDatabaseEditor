using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Modules;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DbScriptsEditor.Models;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Data
{
    [UniqueProvider]
    public interface IDbScriptDataManager
    {
        bool Contains(uint commandId);
        DbScriptCommandDefinition GetCommand(uint commandId);
        DbScriptCommandDefinition? TryGetCommand(uint commandId);
        IReadOnlyList<DbScriptCommandDefinition> AllCommands { get; }
        IReadOnlyList<DbScriptGroupJson> Groups { get; }
        IReadOnlyList<string> ValidationWarnings { get; }
    }

    [AutoRegister]
    [SingleInstance]
    public class DbScriptDataManager : IDbScriptDataManager, IGlobalAsyncInitializer
    {
        private static readonly Regex TokenRegex = new(@"\{([A-Za-z0-9 _]+?)(?::|\})", RegexOptions.Compiled);

        private readonly Dictionary<uint, DbScriptCommandDefinition> byId = new();
        private readonly List<DbScriptCommandDefinition> all = new();
        private readonly List<string> warnings = new();

        private readonly IDbScriptDataProvider provider;
        private readonly IParameterFactory parameterFactory;
        private readonly IMangosConditionService conditionService;
        private readonly IMangosDatabaseProvider databaseProvider;
        private readonly ITableEditorPickerService tableEditorPickerService;

        public IReadOnlyList<DbScriptCommandDefinition> AllCommands => all;
        public IReadOnlyList<DbScriptGroupJson> Groups { get; private set; } = Array.Empty<DbScriptGroupJson>();
        public IReadOnlyList<string> ValidationWarnings => warnings;

        public DbScriptDataManager(IDbScriptDataProvider provider, IParameterFactory parameterFactory,
            IMangosConditionService conditionService,
            IMangosDatabaseProvider databaseProvider,
            ITableEditorPickerService tableEditorPickerService)
        {
            this.provider = provider;
            this.parameterFactory = parameterFactory;
            this.conditionService = conditionService;
            this.databaseProvider = databaseProvider;
            this.tableEditorPickerService = tableEditorPickerService;
        }

        public async Task Initialize()
        {
            RegisterCustomParameters(parameterFactory);
            Groups = await provider.GetGroups();

            foreach (var command in await provider.GetCommands())
            {
                var def = Build(command, parameterFactory);
                if (def == null)
                    continue;
                if (byId.ContainsKey(def.Id))
                {
                    warnings.Add($"Duplicate command id {def.Id} ({def.Name}) — ignored.");
                    continue;
                }
                byId[def.Id] = def;
                all.Add(def);
            }

            foreach (var w in warnings)
                LOG.LogWarning($"[DbScripts] commands.json: {w}");
        }

        public bool Contains(uint commandId) => byId.ContainsKey(commandId);

        public DbScriptCommandDefinition GetCommand(uint commandId) => byId[commandId];

        public DbScriptCommandDefinition? TryGetCommand(uint commandId) =>
            byId.TryGetValue(commandId, out var def) ? def : null;

        private void RegisterCustomParameters(IParameterFactory factory)
        {
            // wotlk: TALK dataint is a broadcast_text id. Kept behind an abstract name so older
            // cores can swap the implementation later. RegisterDepending (not an eager
            // factory.Factory alias) is required: BroadcastTextParameter is registered by the
            // DatabaseEditors module, possibly AFTER this runs — an eager alias would permanently
            // capture the generic fallback (a plain number with no picker).
            if (!factory.IsRegisteredLong("DbScriptTextParameter"))
                factory.RegisterDepending("DbScriptTextParameter", "BroadcastTextParameter", bcast => bcast);
            // Enhanced with live db lookups in a later phase; for now plain numbers.
            if (!factory.IsRegisteredLong("DbScriptRelayParameter"))
                factory.Register("DbScriptRelayParameter", factory.Factory("Parameter"));
            // dbscript_random_templates ids, split per template type so the async display can
            // resolve the values the way the core will interpret them (broadcast texts / relay ids)
            // and the picker can filter the table to the right type.
            if (!factory.IsRegisteredLong(DbScriptEditableParameter.StringRandomTemplateType))
                factory.Register(DbScriptEditableParameter.StringRandomTemplateType,
                    new DbScriptStringRandomTemplateParameter(databaseProvider, tableEditorPickerService));
            if (!factory.IsRegisteredLong(DbScriptEditableParameter.RelayRandomTemplateType))
                factory.Register(DbScriptEditableParameter.RelayRandomTemplateType,
                    new DbScriptRelayRandomTemplateParameter(databaseProvider, tableEditorPickerService));
            // TERMINATE_COND's datalong: a conditions.condition_entry root, editable via the
            // condition tree dialog from the "..." picker in the edit-action window.
            if (!factory.IsRegisteredLong("DbScriptConditionParameter"))
                factory.Register("DbScriptConditionParameter", new DbScriptConditionParameter(conditionService));
        }

        private DbScriptCommandDefinition? Build(DbScriptCommandJson json, IParameterFactory factory)
        {
            if (string.IsNullOrEmpty(json.Name))
            {
                warnings.Add($"Command id {json.Id} has no name — ignored.");
                return null;
            }

            var baseParams = BuildParameters(json.Parameters, json, "base", factory, out var baseOk);
            if (!baseOk)
                return null; // structural error already reported; skip whole command

            ValidateDescription(json.Description, baseParams, json, "base");

            var variants = new List<DbScriptCommandVariant>();
            if (json.Variants != null)
            {
                for (var i = 0; i < json.Variants.Count; i++)
                {
                    var v = json.Variants[i];
                    var vParams = baseParams;
                    if (v.Parameters != null)
                    {
                        vParams = BuildParameters(v.Parameters, json, $"variant '{v.NameReadable}'", factory, out var vOk);
                        if (!vOk)
                            continue;
                    }

                    var presets = BuildPresets(v, json);
                    ValidateDescription(v.Description ?? json.Description, vParams, json, $"variant '{v.NameReadable}'");

                    variants.Add(new DbScriptCommandVariant
                    {
                        NameReadable = v.NameReadable,
                        SearchTags = v.SearchTags,
                        Presets = presets,
                        Parameters = v.Parameters != null ? vParams : null,
                        Description = v.Description,
                        SourceTypes = v.SourceTypes?.ToList(),
                        TargetTypes = v.TargetTypes?.ToList(),
                    });
                }
                // Most-specific first so Resolve() picks the tightest match.
                variants = variants.OrderByDescending(v => v.Presets.Count).ToList();
            }

            return new DbScriptCommandDefinition
            {
                Id = json.Id,
                Name = json.Name,
                NameReadable = string.IsNullOrEmpty(json.NameReadable) ? json.Name : json.NameReadable,
                Help = json.Help,
                SearchTags = json.SearchTags,
                Group = json.Group,
                Deprecated = json.Deprecated,
                SourceTypes = json.SourceTypes?.ToList() ?? new List<string>(),
                TargetTypes = json.TargetTypes?.ToList() ?? new List<string>(),
                Buddy = DbScriptBuddyCapabilities.Parse(json.Buddy),
                PlayerFromSourceOrTarget = json.PlayerSourceOrTarget,
                SupportsAdditionalFlag = json.SupportsAdditionalFlag,
                AdditionalFlag = BuildAdditionalFlag(json, variants),
                Parameters = baseParams,
                Description = json.Description ?? "",
                Variants = variants,
            };
        }

        private List<DbScriptCommandParameter> BuildParameters(
            IList<DbScriptParameterJson>? jsonParams, DbScriptCommandJson command, string where,
            IParameterFactory factory, out bool ok)
        {
            ok = true;
            var result = new List<DbScriptCommandParameter>();
            if (jsonParams == null)
                return result;

            var usedDestinations = new HashSet<DbScriptDestination>();
            var paramIndex = 0;
            foreach (var p in jsonParams)
            {
                paramIndex++;
                if (DbScriptDestinations.Reserved.Contains(p.Destination))
                {
                    warnings.Add($"{command.Name} ({where}): parameter '{p.Name}' uses reserved column '{p.Destination}'.");
                    ok = false;
                    continue;
                }
                if (!DbScriptDestinations.TryParse(p.Destination, out var dest))
                {
                    warnings.Add($"{command.Name} ({where}): parameter '{p.Name}' has invalid destination '{p.Destination}'.");
                    ok = false;
                    continue;
                }
                if (!usedDestinations.Add(dest))
                {
                    warnings.Add($"{command.Name} ({where}): destination '{p.Destination}' used by more than one parameter.");
                    ok = false;
                    continue;
                }

                var isFloatParam = p.Type == "FloatParameter" || factory.IsRegisteredFloat(p.Type);
                if (DbScriptDestinations.IsFloat(dest) != isFloatParam)
                {
                    warnings.Add($"{command.Name} ({where}): parameter '{p.Name}' type '{p.Type}' does not match the class of destination '{p.Destination}'.");
                }

                var type = p.Type;
                if (p.Values is { Count: > 0 })
                {
                    var key = $"DbScriptDyn_{command.Id}_{Sanitize(where)}_{p.Destination}";
                    if (!factory.IsRegisteredLong(key))
                        factory.Register(key, new Parameter { Items = p.Values });
                    type = key;
                }

                result.Add(new DbScriptCommandParameter
                {
                    Name = p.Name,
                    Type = string.IsNullOrEmpty(type) ? "Parameter" : type,
                    Destination = dest,
                    Required = p.Required,
                    DefaultVal = p.DefaultVal,
                });
            }
            return result;
        }

        // The 0x8 switch labels: authored in commands.json (additional_flag), else derived — "on"
        // from the variant whose only preset is the 0x8 data_flags mask.
        private static DbScriptAdditionalFlag? BuildAdditionalFlag(DbScriptCommandJson json, IReadOnlyList<DbScriptCommandVariant> variants)
        {
            if (!json.SupportsAdditionalFlag && json.AdditionalFlag == null)
                return null;

            string? derivedOn = null;
            foreach (var v in variants)
            {
                if (v.Presets.Count == 1 && v.Presets[0].IsDataFlagsMask && (uint)v.Presets[0].Value == 0x8)
                {
                    derivedOn = v.NameReadable;
                    break;
                }
            }

            var af = json.AdditionalFlag;
            return new DbScriptAdditionalFlag
            {
                Name = string.IsNullOrEmpty(af?.Name) ? "Mode" : af!.Value.Name!,
                OffLabel = string.IsNullOrEmpty(af?.Off) ? "Default" : af!.Value.Off!,
                OnLabel = !string.IsNullOrEmpty(af?.On) ? af!.Value.On!
                    : derivedOn ?? "Additional behaviour",
            };
        }

        private List<DbScriptPreset> BuildPresets(DbScriptVariantJson variant, DbScriptCommandJson command)
        {
            var presets = new List<DbScriptPreset>();
            if (variant.Preset == null)
                return presets;
            foreach (var (column, spec) in variant.Preset)
            {
                if (spec.StartsWith("|", StringComparison.Ordinal))
                {
                    if (!column.Equals("data_flags", StringComparison.OrdinalIgnoreCase))
                        warnings.Add($"{command.Name} (variant '{variant.NameReadable}'): OR-mask preset only valid on data_flags, got '{column}'.");
                    presets.Add(new DbScriptPreset { Column = "data_flags", IsDataFlagsMask = true, Value = ParseLong(spec.Substring(1)) });
                }
                else
                {
                    presets.Add(new DbScriptPreset { Column = column, IsDataFlagsMask = false, Value = ParseLong(spec) });
                }
            }
            return presets;
        }

        private void ValidateDescription(string? description, IReadOnlyList<DbScriptCommandParameter> parameters,
            DbScriptCommandJson command, string where)
        {
            if (string.IsNullOrEmpty(description))
                return;
            // Descriptions are SmartFormat templates that reference parameters by destination column
            // ({datalong} for display, {datalongValue} for the raw value used by choose()).
            var paramColumns = new HashSet<string>(
                parameters.Select(p => DbScriptDestinations.ColumnName(p.Destination)), StringComparer.OrdinalIgnoreCase);
            foreach (Match m in TokenRegex.Matches(description))
            {
                var token = m.Groups[1].Value.Trim();
                // actor tokens: {source}/{target}, and {player}/{creature} which resolve to whichever
                // of source/target is a player (target-preferred) / creature (source-preferred).
                if (token is "source" or "target" or "player" or "creature")
                    continue;
                var col = token.EndsWith("Value", StringComparison.Ordinal) ? token[..^5] : token;
                if (DbScriptDestinations.TryParse(col, out _))
                {
                    if (!paramColumns.Contains(col))
                        warnings.Add($"{command.Name} ({where}): references column '{col}' but no parameter maps to it.");
                    continue;
                }
                warnings.Add($"{command.Name} ({where}): description references '{{{token}}}' which is not a parameter column.");
            }
        }

        private static string Sanitize(string s) => Regex.Replace(s, "[^A-Za-z0-9]", "");

        private static long ParseLong(string s)
        {
            s = s.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return long.Parse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }
    }
}
