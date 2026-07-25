using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.DbScriptsEditor.Data;
using WDE.Parameters.Models;

namespace WDE.DbScriptsEditor.Models
{
    // One editable dbscript ACTION row. Every physical column is backed by a value holder so
    // undo/redo and the parameter pickers work uniformly; unmodeled/unused columns keep their raw
    // value so an untouched script roundtrips byte-identically.
    // Delay is DERIVED (EditableDbScript.SyncDerived accumulates the preceding wait rows into it);
    // comments are separate DbScriptCommentRow entities, not part of the action.
    public class EditableDbScriptStep : DbScriptRow
    {
        private static readonly DbScriptDestination[] LongDataDestinations =
        {
            DbScriptDestination.DataLong, DbScriptDestination.DataLong2, DbScriptDestination.DataLong3,
            DbScriptDestination.DataInt, DbScriptDestination.DataInt2, DbScriptDestination.DataInt3, DbScriptDestination.DataInt4,
        };

        private static readonly DbScriptDestination[] FloatDataDestinations =
        {
            DbScriptDestination.DataFloat, DbScriptDestination.X, DbScriptDestination.Y,
            DbScriptDestination.Z, DbScriptDestination.O, DbScriptDestination.Speed,
        };

        private readonly IDbScriptDataManager dataManager;
        private readonly DbScriptTypeInfo typeInfo;
        private readonly IParameterFactory factory;

        private readonly Dictionary<DbScriptDestination, ParameterValueHolder<long>> longColumns = new();
        private readonly Dictionary<DbScriptDestination, ParameterValueHolder<float>> floatColumns = new();

        // The COMMAND_ADDITIONAL (data_flags 0x8) bit surfaced as a synthetic two-option switch
        // parameter. It is not a physical column — it reads/writes bit 0x8 of DataFlags (which is
        // the column that goes to history), so undo/redo and roundtripping keep working.
        private const uint CommandAdditionalBit = 0x8;
        private readonly ParameterValueHolder<long> additionalFlagHolder;
        private DbScriptEditableParameter? additionalFlagParam;
        private bool syncingAdditionalFlag;
        private bool additionalFlagCoveredByVariant;

        public uint RowId { get; set; }
        public uint Priority { get; set; }

        public ParameterValueHolder<long> Delay { get; }
        public ParameterValueHolder<long> BuddyEntry { get; }
        public ParameterValueHolder<long> SearchRadius { get; }
        public ParameterValueHolder<long> DataFlags { get; }
        public ParameterValueHolder<long> ConditionId { get; }

        public ObservableCollection<DbScriptEditableParameter> UsedParameters { get; } = new();
        public ObservableCollection<DbScriptEditableParameter> AdvancedParameters { get; } = new();

        // Emitted when the command id changes; the history handler pushes an undo entry.
        public event Action<EditableDbScriptStep, uint, uint>? CommandChanged;

        private uint commandId;
        public uint CommandId
        {
            get => commandId;
            set
            {
                if (commandId == value)
                    return;
                var old = commandId;
                commandId = value;
                RaisePropertyChanged();
                CommandChanged?.Invoke(this, old, value);
                Recompute();
            }
        }

        private string commandName = "";
        public string CommandName { get => commandName; private set => SetProperty(ref commandName, value); }

        private string? variantName;
        public string? VariantName { get => variantName; private set => SetProperty(ref variantName, value); }

        private string readable = "";
        public string Readable { get => readable; private set => SetProperty(ref readable, value); }

        // Readable with FormattedTextBlock markup: parameters become clickable [p=N] links whose N
        // indexes ReadableContext; source/target render as styled (non-clickable) [s] spans.
        private string formattedReadable = "";
        public string FormattedReadable { get => formattedReadable; private set => SetProperty(ref formattedReadable, value); }

        private IReadOnlyList<object> readableContext = Array.Empty<object>();
        public IReadOnlyList<object> ReadableContext { get => readableContext; private set => SetProperty(ref readableContext, value); }

        private static readonly Regex TokenRegex = new(@"\{([A-Za-z0-9 _]+?)\}", RegexOptions.Compiled);

        private IReadOnlyList<string> unusedColumns = Array.Empty<string>();
        public IReadOnlyList<string> UnusedColumns { get => unusedColumns; private set => SetProperty(ref unusedColumns, value); }

        private string paramSignature = "";
        // The command description last used to render the readable; kept so the readable can be
        // rebuilt when an async parameter (e.g. broadcast text) resolves its display string later.
        private string lastDescription = "";

        // Fired when a data-column holder's async ToString (e.g. BroadcastTextParameter → the actual
        // text) resolves. Reads are cached by then, so rebuilding won't re-trigger the async.
        private void OnHolderStringResolved(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IParameterValueHolder.String) && lastDescription.Length > 0)
                BuildFormattedReadable(lastDescription);
        }

        public EditableDbScriptStep(IDbScriptLine row, IDbScriptDataManager dataManager, DbScriptTypeInfo typeInfo, IParameterFactory factory)
        {
            this.dataManager = dataManager;
            this.typeInfo = typeInfo;
            this.factory = factory;

            RowId = row.Id;
            Priority = row.Priority;
            commandId = row.Command;

            Delay = new ParameterValueHolder<long>("Delay", factory.Factory(null), row.Delay);
            BuddyEntry = new ParameterValueHolder<long>("Buddy entry", factory.Factory(null), row.BuddyEntry);
            SearchRadius = new ParameterValueHolder<long>("Search radius", factory.Factory(null), row.SearchRadius);
            DataFlags = new ParameterValueHolder<long>("Data flags", factory.Factory(null), row.DataFlags);
            ConditionId = new ParameterValueHolder<long>("Condition id", factory.Factory(null), row.ConditionId);

            foreach (var d in LongDataDestinations)
            {
                var holder = new ParameterValueHolder<long>(d.ToString(), factory.Factory(null), DbScriptDestinations.ReadLong(row, d));
                longColumns[d] = holder;
                holder.OnValueChanged += (_, _, _) => Recompute();
                holder.PropertyChanged += OnHolderStringResolved;
            }
            foreach (var d in FloatDataDestinations)
            {
                var holder = new ParameterValueHolder<float>(d.ToString(), factory.FactoryFloat(null), DbScriptDestinations.ReadFloat(row, d));
                floatColumns[d] = holder;
                holder.OnValueChanged += (_, _, _) => Recompute();
            }

            additionalFlagHolder = new ParameterValueHolder<long>("Mode", factory.Factory(null), 0);
            additionalFlagHolder.OnValueChanged += (_, _, _) =>
            {
                if (syncingAdditionalFlag)
                    return;
                var flags = (uint)DataFlags.Value;
                DataFlags.Value = additionalFlagHolder.Value != 0
                    ? (flags | CommandAdditionalBit)
                    : (flags & ~CommandAdditionalBit);
            };

            // structural columns can change the resolved variant (e.g. data_flags masks) and the readable
            DataFlags.OnValueChanged += (_, _, _) => Recompute();
            BuddyEntry.OnValueChanged += (_, _, _) => Recompute();
            SearchRadius.OnValueChanged += (_, _, _) => Recompute();
            // the buddy entry resolves to a creature/GO name asynchronously like data columns do
            BuddyEntry.PropertyChanged += OnHolderStringResolved;

            // Buddy entry / search radius / data flags are edited structurally (phase 3); delay is
            // derived from wait rows and condition_id from the enclosing "if" row — neither is
            // directly editable on the step anymore.

            Recompute();
        }

        public IEnumerable<ParameterValueHolder<long>> AllLongHolders
        {
            get
            {
                yield return Delay;
                yield return BuddyEntry;
                yield return SearchRadius;
                yield return DataFlags;
                yield return ConditionId;
                foreach (var h in longColumns.Values)
                    yield return h;
            }
        }

        public IEnumerable<ParameterValueHolder<float>> AllFloatHolders => floatColumns.Values;

        // Data-column holders in physical column order — the edit dialog lists every one and the
        // per-row IsUsed/value visibility hides the unmapped ones (EventAI-style), so the dialog
        // reshapes itself live when the command changes.
        public IEnumerable<ParameterValueHolder<long>> DataLongHolders =>
            LongDataDestinations.Select(d => longColumns[d]);
        public IEnumerable<ParameterValueHolder<float>> DataFloatHolders =>
            FloatDataDestinations.Select(d => floatColumns[d]);

        public ParameterValueHolder<long> GetLongColumn(DbScriptDestination d) => longColumns[d];
        public ParameterValueHolder<float> GetFloatColumn(DbScriptDestination d) => floatColumns[d];

        // The resolved command definition and script type, used by the structural source/target
        // editor for capabilities (buddy kind, source/target types) and contextual labels.
        public DbScriptCommandDefinition? Command => dataManager.TryGetCommand(CommandId);
        public DbScriptTypeInfo TypeInfo => typeInfo;

        // The synthetic 0x8 switch, exposed so the edit dialog can show it as a regular two-option
        // parameter row. IsUsed tracks whether the current command supports the flag.
        public ParameterValueHolder<long> AdditionalFlagHolder => additionalFlagHolder;

        // Copies every physical column from another line into this step's holders in one undo step.
        // Used by the "edit action" dialog, which edits a detached copy and applies on Accept.
        public void CopyFrom(IDbScriptLine line)
        {
            using (BulkEdit("Edit action"))
            {
                CommandId = line.Command;
                Priority = line.Priority;
                BuddyEntry.Value = line.BuddyEntry;
                SearchRadius.Value = line.SearchRadius;
                DataFlags.Value = line.DataFlags;
                ConditionId.Value = line.ConditionId;
                foreach (var d in LongDataDestinations)
                    longColumns[d].Value = DbScriptDestinations.ReadLong(line, d);
                foreach (var d in FloatDataDestinations)
                    floatColumns[d].Value = DbScriptDestinations.ReadFloat(line, d);
            }
            // The per-holder change handlers already recompute, but only for values that actually
            // differed; a final pass guarantees the readable reflects the fully-applied line even
            // when the last write was a no-op.
            Recompute();
        }

        // Condition edits made in the edit-action dialog (which edits a detached copy of the
        // step); the editor VM moves them into the document's conditions store on Accept,
        // Cancel simply discards them with the copy.
        public List<DbScriptPendingConditionEdit>? PendingConditionEdits { get; set; }

        // Set by the editor VM on the dialog copy: resolves a condition root to the document's
        // (possibly edited, unsaved) closure, so re-editing doesn't reload stale database rows.
        // null result = the document knows nothing about this root, load from the database.
        public Func<uint, IReadOnlyList<IMangosConditionLine>?>? ConditionClosureProvider { get; set; }

        // Human-readable resolved source/target ("who acts on whom") for display.
        public (string source, string target) ResolveActors() => DbScriptActorResolver.Resolve(ToLine(), typeInfo, ResolveBuddyName);

        // Resolves a buddy entry to a creature/GO name (CreatureParameter / GameobjectParameter),
        // so "buddy 1234" shows the creature's name instead of a raw id.
        private IParameter<long>? creatureNameParam;
        private IParameter<long>? gameobjectNameParam;
        private string ResolveBuddyName(long entry, bool isGameObject)
        {
            if (entry == 0)
                return entry.ToString();
            var param = isGameObject
                ? (gameobjectNameParam ??= factory.Factory("GameobjectParameter"))
                : (creatureNameParam ??= factory.Factory("CreatureParameter"));
            return param.ToString(entry);
        }

        // Structural source/target/buddy state, decoded from the physical columns.
        public DecodedFlags DecodeFlags() =>
            DbScriptFlagsCodec.Decode((uint)DataFlags.Value, BuddyEntry.Value, SearchRadius.Value);

        // Writes a compiled structural view back to the three physical columns in one undo step.
        public void ApplyDecodedFlags(in DecodedFlags decoded)
        {
            if (!DbScriptFlagsCodec.TryEncode(decoded, out var dataFlags, out var buddyEntry, out var searchRadius))
                return;
            using (BulkEdit("Edit source / target"))
            {
                DataFlags.Value = dataFlags;
                BuddyEntry.Value = buddyEntry;
                SearchRadius.Value = searchRadius;
            }
        }

        // Applies a variant's presets (data_flags bits / exact column values) so picking a
        // variant from the command list pre-fills its discriminating columns.
        public void ApplyVariantPresets(DbScriptCommandVariant variant)
        {
            foreach (var preset in variant.Presets)
            {
                if (preset.IsDataFlagsMask)
                    DataFlags.Value |= (uint)preset.Value;
                else if (DbScriptDestinations.TryParse(preset.Column, out var dest))
                {
                    if (DbScriptDestinations.IsFloat(dest))
                        floatColumns[dest].Value = (float)preset.Value;
                    else
                        longColumns[dest].Value = preset.Value;
                }
            }
        }

        // Pre-fills every resolved parameter's non-zero defaultVal (e.g. seat index -1 = all
        // seats). Only meant for brand-new actions — it overwrites the mapped columns.
        public void ApplyParameterDefaults()
        {
            var def = dataManager.TryGetCommand(CommandId);
            if (def == null)
                return;
            var (parameters, _, _) = def.Resolve(ToLine());
            using (BulkEdit("Apply parameter defaults"))
            {
                foreach (var p in parameters)
                {
                    if (p.DefaultVal == 0)
                        continue;
                    if (DbScriptDestinations.IsFloat(p.Destination))
                        floatColumns[p.Destination].Value = p.DefaultVal;
                    else
                        longColumns[p.Destination].Value = p.DefaultVal;
                }
            }
        }

        public AbstractDbScriptLine ToLine() => new()
        {
            Id = RowId,
            Delay = (uint)Delay.Value,
            Priority = Priority,
            Command = CommandId,
            DataLong = (uint)longColumns[DbScriptDestination.DataLong].Value,
            DataLong2 = (uint)longColumns[DbScriptDestination.DataLong2].Value,
            DataLong3 = (uint)longColumns[DbScriptDestination.DataLong3].Value,
            BuddyEntry = (uint)BuddyEntry.Value,
            SearchRadius = (uint)SearchRadius.Value,
            DataFlags = (uint)DataFlags.Value,
            DataInt = (int)longColumns[DbScriptDestination.DataInt].Value,
            DataInt2 = (int)longColumns[DbScriptDestination.DataInt2].Value,
            DataInt3 = (int)longColumns[DbScriptDestination.DataInt3].Value,
            DataInt4 = (int)longColumns[DbScriptDestination.DataInt4].Value,
            DataFloat = floatColumns[DbScriptDestination.DataFloat].Value,
            X = floatColumns[DbScriptDestination.X].Value,
            Y = floatColumns[DbScriptDestination.Y].Value,
            Z = floatColumns[DbScriptDestination.Z].Value,
            O = floatColumns[DbScriptDestination.O].Value,
            Speed = floatColumns[DbScriptDestination.Speed].Value,
            ConditionId = (uint)ConditionId.Value,
            Comments = null, // comments are separate rows; the exporter composes the column
        };

        // Reshapes the buddy holders to the decoded buddy mode: name, mapped parameter type
        // (creature/GO picker for entries, plain number for guid/pool/radius) and IsUsed. This
        // makes "buddy entry" / "search radius" behave like regular command parameters — editable
        // rows in the edit dialog and clickable values in the readable — without re-picking the
        // source/target kind.
        private string buddySignature = "";
        private DbScriptEditableParameter? buddyEntryParam;
        private DbScriptEditableParameter? buddySearchParam;

        private void UpdateBuddyHolders(in DecodedFlags decoded)
        {
            var usesBuddy = decoded.Direction.UsesBuddy || decoded.Buddy.Provided;
            var mode = decoded.Buddy.Provided ? decoded.Buddy.Mode : BuddyFindMode.NearestByEntry;
            var isGo = decoded.Buddy.IsGameObject;

            var signature = $"{usesBuddy}|{mode}|{isGo}";
            if (signature == buddySignature)
                return;
            buddySignature = signature;

            if (!usesBuddy)
            {
                BuddyEntry.Name = "Buddy entry";
                BuddyEntry.Parameter = factory.Factory(null);
                BuddyEntry.IsUsed = false;
                SearchRadius.Name = "Search radius";
                SearchRadius.Parameter = factory.Factory(null);
                SearchRadius.IsUsed = false;
                buddyEntryParam = null;
                buddySearchParam = null;
                return;
            }

            var entityParam = factory.Factory(isGo ? "GameobjectParameter" : "CreatureParameter");
            var plain = factory.Factory(null);

            // search_radius is reinterpreted per locator mode (yards / guid / pool id); null = unused
            (string entryName, IParameter<long> entryParam, string? searchName) = mode switch
            {
                BuddyFindMode.ByGuid => ("Buddy expected entry (0 = any)", entityParam, "Buddy GUID"),
                BuddyFindMode.ByPool => ("Buddy expected entry (0 = any)", entityParam, "Pool id"),
                BuddyFindMode.BySpawnGroup => ("Spawn group id", plain, "Search radius (yd)"),
                BuddyFindMode.ByStringId => ("String id", plain, "Search radius (yd)"),
                BuddyFindMode.Pet => ("Pet of (creature entry)", entityParam, null),
                _ => (isGo ? "Buddy gameobject entry" : "Buddy creature entry", entityParam, "Search radius (yd)"),
            };

            BuddyEntry.Name = entryName;
            BuddyEntry.Parameter = entryParam;
            BuddyEntry.IsUsed = true;

            SearchRadius.Name = searchName ?? "Search radius";
            SearchRadius.Parameter = plain;
            SearchRadius.IsUsed = searchName != null;

            buddyEntryParam = new DbScriptEditableParameter(entryName, null, false, BuddyEntry, null);
            buddySearchParam = searchName != null
                ? new DbScriptEditableParameter(searchName, null, false, SearchRadius, null)
                : null;
        }

        private void Recompute()
        {
            var line = ToLine();
            var def = dataManager.TryGetCommand(CommandId);
            var step = new DbScriptStep(line, def, typeInfo, factory);

            CommandName = step.CommandName;
            VariantName = step.VariantName;
            Readable = step.Readable;
            UnusedColumns = step.UnusedColumns;

            IReadOnlyList<DbScriptCommandParameter> parameters = Array.Empty<DbScriptCommandParameter>();
            DbScriptCommandVariant? variant = null;
            if (def != null)
                (parameters, _, variant) = def.Resolve(line);

            // When the resolved variant is the 0x8 one (and has its own description), the sentence
            // already expresses the additional behaviour and the trailing switch chip is redundant.
            additionalFlagCoveredByVariant = variant != null && variant.Description != null &&
                variant.Presets.Any(p => p.IsDataFlagsMask && ((uint)p.Value & CommandAdditionalBit) != 0);

            var signature = $"{CommandId}|{variant?.NameReadable}|{parameters.Count}";
            if (signature != paramSignature)
            {
                paramSignature = signature;
                RemapHolders(parameters);
                SetupAdditionalFlag(def);
            }

            SyncAdditionalFlagFromBit();
            UpdateBuddyHolders(DecodeFlags());

            // Rebuilt every recompute (parameter VALUES change without changing the signature).
            lastDescription = def != null ? def.Resolve(line).description : Readable;
            BuildFormattedReadable(lastDescription);
        }

        // Builds (or clears) the 0x8 switch parameter when the command changes.
        private void SetupAdditionalFlag(DbScriptCommandDefinition? def)
        {
            if (def?.AdditionalFlag is { } af)
            {
                additionalFlagHolder.Name = af.Name;
                additionalFlagHolder.Parameter = new Parameter
                {
                    Items = new Dictionary<long, SelectOption>
                    {
                        [0] = new SelectOption(af.OffLabel),
                        [1] = new SelectOption(af.OnLabel),
                    }
                };
                additionalFlagHolder.IsUsed = true;
                additionalFlagParam = new DbScriptEditableParameter(af.Name, null, false, additionalFlagHolder, null);
            }
            else
            {
                additionalFlagHolder.IsUsed = false;
                additionalFlagParam = null;
                // Zero the display value (without touching DataFlags) so the hidden switch doesn't
                // linger visible in the edit dialog for commands that don't support the flag.
                syncingAdditionalFlag = true;
                additionalFlagHolder.Value = 0;
                syncingAdditionalFlag = false;
            }
        }

        private void SyncAdditionalFlagFromBit()
        {
            if (additionalFlagParam == null)
                return;
            syncingAdditionalFlag = true;
            additionalFlagHolder.Value = ((uint)DataFlags.Value & CommandAdditionalBit) != 0 ? 1 : 0;
            syncingAdditionalFlag = false;
        }

        // Produces FormattedTextBlock markup from the command description, turning each used
        // parameter into a clickable [p=N] link (N indexing ReadableContext) and source/target
        // into styled [s] spans. Mirrors the read-only renderer but for the live editable holders.
        private void BuildFormattedReadable(string description)
        {
            var context = new List<object>();
            var friendly = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Source and target render as green [s=N] spans that are ALSO clickable (the shared
            // FormattedTextBlock makes any span with a context id a link) — clicking opens the
            // high-level actor picker, which compiles the choice down to flags + buddy. A buddy
            // slot additionally embeds its own values (entry, radius/guid/pool) as separate [p]
            // parameter links, SmartScript-style, so they edit directly without re-picking the
            // kind. Commands that don't consume an actor render it as plain, non-clickable text.
            var def2 = dataManager.TryGetCommand(CommandId);
            var decoded = DecodeFlags();
            friendly["source"] = RenderActorSlot(context, def2, decoded, true);
            friendly["target"] = RenderActorSlot(context, def2, decoded, false);

            foreach (var p in UsedParameters)
            {
                var index = context.Count;
                friendly[p.Name] = $"[p={index}]{Escape(p.Holder.String)}[/p]";
                context.Add(p);
            }

            // referenced is computed from the un-expanded description on purpose: a parameter
            // mentioned only inside a non-chosen choose branch still counts as "mentioned".
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in TokenRegex.Matches(description))
                referenced.Add(m.Groups[1].Value.Trim());

            description = DbScriptDescriptionChoose.Expand(description, name =>
            {
                var p = UsedParameters.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (p == null)
                    return null;
                return p.IsFloat
                    ? p.FloatHolder!.Value.ToString("0.###", CultureInfo.InvariantCulture)
                    : p.LongHolder!.Value.ToString(CultureInfo.InvariantCulture);
            });

            var rendered = TokenRegex.Replace(description, m =>
            {
                var token = m.Groups[1].Value.Trim();
                return friendly.TryGetValue(token, out var value) ? value : m.Value;
            });

            // Append any set-but-unmentioned parameter as an extra clickable chip so nothing hides.
            var extras = UsedParameters
                .Where(p => !referenced.Contains(p.Name) && IsNonDefault(p))
                .Select(p => $"{p.Name}: {friendly[p.Name]}")
                .ToList();
            if (extras.Count > 0)
                rendered = $"{rendered} · {string.Join(", ", extras)}";

            // The 0x8 switch shows as a trailing clickable token only when the flag is SET and the
            // resolved variant's sentence doesn't already express it (commands 20/37 have no 0x8
            // variant description). The default/off state stays out of the sentence — the switch is
            // always editable as a row in the edit-action dialog.
            if (additionalFlagParam != null && additionalFlagHolder.Value != 0 && !additionalFlagCoveredByVariant)
            {
                var index = context.Count;
                context.Add(additionalFlagParam);
                rendered = $"{rendered} · [p={index}]{Escape(additionalFlagHolder.String)}[/p]";
            }

            ReadableContext = context;
            FormattedReadable = rendered;
        }

        // One source/target slot of the sentence: the kind label is the actor-picker link, a buddy
        // slot's values are embedded parameter links of their own.
        private string RenderActorSlot(List<object> context, DbScriptCommandDefinition? def,
            in DecodedFlags decoded, bool isSource)
        {
            var clickable = def == null || (isSource ? def.UsesSource : def.UsesTarget);
            var kind = isSource ? decoded.Direction.Source : decoded.Direction.Target;

            string open;
            if (clickable)
            {
                open = $"[s={context.Count}]";
                context.Add(new DbScriptActorSlot(this, isSource));
            }
            else
                open = "[s]";

            if (kind != SourceTargetKind.Buddy)
            {
                var label = kind == SourceTargetKind.OriginalSource ? typeInfo.SourceLabel : typeInfo.TargetLabel;
                return $"{open}{Escape(label)}[/s]";
            }

            return RenderBuddy(context, decoded.Buddy, open, clickable);
        }

        private string RenderBuddy(List<object> context, in BuddyDescriptor buddy, string open, bool clickable)
        {
            var entry = Link(context, buddyEntryParam, BuddyEntry);
            string SearchLink() => Link(context, buddySearchParam, SearchRadius);

            var mode = buddy.Provided ? buddy.Mode : BuddyFindMode.NearestByEntry;
            var core = mode switch
            {
                BuddyFindMode.ByGuid => $"{open}buddy by guid[/s] {SearchLink()} (entry {entry})",
                BuddyFindMode.ByPool => $"{open}buddy from pool[/s] {SearchLink()} (entry {entry})",
                BuddyFindMode.BySpawnGroup => $"{open}buddy from spawn group[/s] {entry} (within {SearchLink()} yd)",
                BuddyFindMode.ByStringId => $"{open}buddy by string id[/s] {entry} (within {SearchLink()} yd)",
                BuddyFindMode.Pet => $"{open}pet of[/s] {entry}",
                _ => $"{open}nearest{(buddy.IsGameObject ? " GO" : "")}[/s] {entry} (within {SearchLink()} yd)",
            };

            if (buddy.AllEligible)
                core += " (all eligible)";
            if (buddy.IncludeDespawned)
                core += " (incl. dead)";
            return core;
        }

        // A clickable [p=N] value link for a buddy holder; plain text when the wrapper is absent
        // (mode doesn't use the column).
        private static string Link(List<object> context, DbScriptEditableParameter? param, ParameterValueHolder<long> holder)
        {
            if (param == null)
                return Escape(holder.String);
            var index = context.Count;
            context.Add(param);
            return $"[p={index}]{Escape(holder.String)}[/p]";
        }

        private static bool IsNonDefault(DbScriptEditableParameter p) =>
            p.IsFloat ? Math.Abs(p.FloatHolder!.Value - p.DefaultVal) > float.Epsilon : p.LongHolder!.Value != p.DefaultVal;

        // FormattedTextBlock uses '[' and '\\' as markup control characters; escape them in values.
        private static string Escape(string value) =>
            value.Replace("\\", "\\\\").Replace("[", "\\[");

        private void RemapHolders(IReadOnlyList<DbScriptCommandParameter> parameters)
        {
            // reset all data columns to unmapped/generic, then apply the resolved mapping in order
            foreach (var (d, holder) in longColumns)
            {
                holder.Name = d.ToString();
                holder.IsUsed = false;
                holder.Parameter = factory.Factory(null);
            }
            foreach (var (d, holder) in floatColumns)
            {
                holder.Name = d.ToString();
                holder.IsUsed = false;
                holder.Parameter = factory.FactoryFloat(null);
            }

            UsedParameters.Clear();
            foreach (var p in parameters)
            {
                if (DbScriptDestinations.IsFloat(p.Destination))
                {
                    var holder = floatColumns[p.Destination];
                    holder.Name = p.Name;
                    holder.IsUsed = true;
                    holder.Parameter = factory.FactoryFloat(p.Type);
                    UsedParameters.Add(new DbScriptEditableParameter(p.Name, p.Destination, true, null, holder, p.Type, p.DefaultVal));
                }
                else
                {
                    var holder = longColumns[p.Destination];
                    holder.Name = p.Name;
                    holder.IsUsed = true;
                    holder.Parameter = factory.Factory(p.Type);
                    UsedParameters.Add(new DbScriptEditableParameter(p.Name, p.Destination, false, holder, null, p.Type, p.DefaultVal));
                }
            }
        }
    }
}
