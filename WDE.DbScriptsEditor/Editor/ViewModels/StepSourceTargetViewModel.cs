using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Disposables;
using WDE.Common.Utils;
using WDE.DbScriptsEditor.Models;
using WDE.MVVM;
using WDE.Parameters.Models;

namespace WDE.DbScriptsEditor.Editor.ViewModels
{
    // A picked source/target combination, labelled with this script type's contextual names.
    public class ScriptDirectionOption
    {
        public ScriptDirection Direction { get; }
        public string Label { get; }

        public ScriptDirectionOption(ScriptDirection direction, string label)
        {
            Direction = direction;
            Label = label;
        }
    }

    public class BuddyModeOption
    {
        public BuddyFindMode Mode { get; }
        public string Label { get; }

        public BuddyModeOption(BuddyFindMode mode, string label)
        {
            Mode = mode;
            Label = label;
        }
    }

    // The phase-3 structural editor: presents Source/Target and the buddy descriptor instead of
    // raw data_flags/buddy_entry/search_radius. Writes are re-encoded through the step's existing
    // holders (so undo/redo and unmodeled-bit passthrough keep working) and grouped as one edit.
    public class StepSourceTargetViewModel : ObservableBase
    {
        private readonly EditableDbScriptStep step;
        private readonly Func<bool, long, Task<(long value, bool ok)>>? buddyEntryPicker;
        private bool refreshing;
        private bool committing;

        public StepSourceTargetViewModel(EditableDbScriptStep step,
            Func<bool, long, Task<(long value, bool ok)>>? buddyEntryPicker = null)
        {
            this.step = step;
            this.buddyEntryPicker = buddyEntryPicker;
            PickBuddyEntry = new AsyncAutoCommand(PickBuddyEntryAsync, () => buddyEntryPicker != null);
            step.DataFlags.OnValueChanged += OnHolderChanged;
            step.BuddyEntry.OnValueChanged += OnHolderChanged;
            step.SearchRadius.OnValueChanged += OnHolderChanged;
            step.PropertyChanged += OnStepPropertyChanged;
            AutoDispose(new ActionDisposable(() =>
            {
                step.DataFlags.OnValueChanged -= OnHolderChanged;
                step.BuddyEntry.OnValueChanged -= OnHolderChanged;
                step.SearchRadius.OnValueChanged -= OnHolderChanged;
                step.PropertyChanged -= OnStepPropertyChanged;
            }));
            Refresh();
        }

        public static readonly IReadOnlyList<BuddyModeOption> AllBuddyModes = new[]
        {
            new BuddyModeOption(BuddyFindMode.None, "No buddy"),
            new BuddyModeOption(BuddyFindMode.NearestByEntry, "Nearest by entry"),
            new BuddyModeOption(BuddyFindMode.ByGuid, "By GUID"),
            new BuddyModeOption(BuddyFindMode.ByPool, "By pool"),
            new BuddyModeOption(BuddyFindMode.BySpawnGroup, "By spawn group (NYI)"),
            new BuddyModeOption(BuddyFindMode.ByStringId, "By string id"),
            new BuddyModeOption(BuddyFindMode.Pet, "Pet"),
        };

        public IReadOnlyList<BuddyModeOption> BuddyModes => AllBuddyModes;

        private IReadOnlyList<ScriptDirectionOption> directionOptions = Array.Empty<ScriptDirectionOption>();
        public IReadOnlyList<ScriptDirectionOption> DirectionOptions
        {
            get => directionOptions;
            private set => SetProperty(ref directionOptions, value);
        }

        private ScriptDirectionOption? selectedDirection;
        public ScriptDirectionOption? SelectedDirection
        {
            get => selectedDirection;
            set
            {
                if (SetProperty(ref selectedDirection, value) && value != null)
                    Commit();
            }
        }

        private BuddyModeOption selectedBuddyMode = AllBuddyModes[0];
        public BuddyModeOption SelectedBuddyMode
        {
            get => selectedBuddyMode;
            set
            {
                if (SetProperty(ref selectedBuddyMode, value))
                    Commit();
            }
        }

        private long buddyEntry;
        public long BuddyEntry
        {
            get => buddyEntry;
            set { if (SetProperty(ref buddyEntry, value)) Commit(); }
        }

        // Opens the creature/GO entity picker for the buddy entry (wired from the editor VM which
        // owns the picker service). Enabled only when a picker was provided.
        public AsyncAutoCommand PickBuddyEntry { get; }
        public bool CanPickBuddyEntry => buddyEntryPicker != null;

        private async Task PickBuddyEntryAsync()
        {
            if (buddyEntryPicker == null)
                return;
            var (value, ok) = await buddyEntryPicker(isGameObjectBuddy, buddyEntry);
            if (ok)
                BuddyEntry = value;
        }

        private long buddySearchValue;
        public long BuddySearchValue
        {
            get => buddySearchValue;
            set { if (SetProperty(ref buddySearchValue, value)) Commit(); }
        }

        private bool isGameObjectBuddy;
        public bool IsGameObjectBuddy
        {
            get => isGameObjectBuddy;
            set { if (SetProperty(ref isGameObjectBuddy, value)) Commit(); }
        }

        private bool includeDespawned;
        public bool IncludeDespawned
        {
            get => includeDespawned;
            set { if (SetProperty(ref includeDespawned, value)) Commit(); }
        }

        private bool allEligible;
        public bool AllEligible
        {
            get => allEligible;
            set { if (SetProperty(ref allEligible, value)) Commit(); }
        }

        private bool commandAdditional;
        public bool CommandAdditional
        {
            get => commandAdditional;
            set { if (SetProperty(ref commandAdditional, value)) Commit(); }
        }

        // Derived UI hints -----------------------------------------------------------------
        public bool ShowBuddyFields => selectedBuddyMode.Mode != BuddyFindMode.None;
        public bool ShowEntry => selectedBuddyMode.Mode != BuddyFindMode.None;
        public bool ShowSearchValue =>
            selectedBuddyMode.Mode is BuddyFindMode.NearestByEntry or BuddyFindMode.ByGuid or BuddyFindMode.ByPool;
        public bool ShowGoToggle =>
            selectedBuddyMode.Mode is BuddyFindMode.NearestByEntry or BuddyFindMode.ByGuid or BuddyFindMode.ByPool;

        public string EntryLabel => selectedBuddyMode.Mode switch
        {
            BuddyFindMode.ByGuid => "Expected entry",
            BuddyFindMode.BySpawnGroup => "Spawn group id",
            BuddyFindMode.ByStringId => "String id",
            _ => "Entry",
        };

        public string SearchValueLabel => selectedBuddyMode.Mode switch
        {
            BuddyFindMode.ByGuid => "GUID",
            BuddyFindMode.ByPool => "Pool id",
            _ => "Radius (yd)",
        };

        public bool SupportsAdditional => step.Command?.SupportsAdditionalFlag ?? false;

        private string rawFlagsText = "";
        public string RawFlagsText { get => rawFlagsText; private set => SetProperty(ref rawFlagsText, value); }

        private IReadOnlyList<string> warnings = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get => warnings; private set => SetProperty(ref warnings, value); }
        public bool HasWarnings => warnings.Count > 0;

        private void OnHolderChanged(ParameterValueHolder<long> h, long old, long @new)
        {
            if (!committing)
                Refresh();
        }

        private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(EditableDbScriptStep.CommandName) or nameof(EditableDbScriptStep.VariantName))
                Refresh();
        }

        // Reads the current holders and repopulates every exposed property without writing back.
        private void Refresh()
        {
            refreshing = true;
            try
            {
                var decoded = DbScriptFlagsCodec.Decode(
                    (uint)step.DataFlags.Value, step.BuddyEntry.Value, step.SearchRadius.Value);

                var buddy = decoded.Buddy;
                selectedBuddyMode = AllBuddyModes.FirstOrDefault(m => m.Mode == buddy.Mode) ?? AllBuddyModes[0];
                buddyEntry = buddy.Entry;
                buddySearchValue = buddy.SearchValue;
                isGameObjectBuddy = buddy.IsGameObject;
                includeDespawned = buddy.IncludeDespawned;
                allEligible = buddy.AllEligible;
                commandAdditional = decoded.CommandAdditional;

                DirectionOptions = BuildDirectionOptions(buddy.Provided);
                selectedDirection = DirectionOptions.FirstOrDefault(o => o.Direction == decoded.Direction)
                                    ?? DirectionOptions.FirstOrDefault();

                RawFlagsText = BuildRawFlagsText(decoded);
                Warnings = DbScriptStructuralValidator.Validate(decoded.Direction, buddy, step.Command);

                RaiseAll();
            }
            finally
            {
                refreshing = false;
            }
        }

        private void Commit()
        {
            if (refreshing)
                return;

            var buddy = BuildBuddyDescriptor();
            var provided = buddy.Provided;

            // Keep the current unmodeled bits from whatever is stored right now.
            var current = DbScriptFlagsCodec.Decode(
                (uint)step.DataFlags.Value, step.BuddyEntry.Value, step.SearchRadius.Value);

            var direction = selectedDirection?.Direction ?? new ScriptDirection(SourceTargetKind.OriginalSource, SourceTargetKind.OriginalTarget);

            var candidate = new DecodedFlags(direction, commandAdditional, buddy, current.UnmodeledBits);
            if (!DbScriptFlagsCodec.TryEncode(candidate, out var flags, out var entry, out var radius))
            {
                // The stored direction is no longer valid for the new buddy state; fall back to the
                // first direction the codec allows so we always produce a representable row.
                direction = DbScriptFlagsCodec.AllowedDirections(provided).First();
                candidate = new DecodedFlags(direction, commandAdditional, buddy, current.UnmodeledBits);
                DbScriptFlagsCodec.TryEncode(candidate, out flags, out entry, out radius);
            }

            committing = true;
            try
            {
                using (step.BulkEdit("Edit source / target"))
                {
                    step.DataFlags.Value = flags;
                    step.BuddyEntry.Value = entry;
                    step.SearchRadius.Value = radius;
                }
            }
            finally
            {
                committing = false;
            }

            Refresh();
        }

        private BuddyDescriptor BuildBuddyDescriptor()
        {
            var mode = selectedBuddyMode.Mode;
            if (mode == BuddyFindMode.None)
                return BuddyDescriptor.None;
            return new BuddyDescriptor(mode, isGameObjectBuddy, buddyEntry, buddySearchValue, includeDespawned, allEligible);
        }

        private IReadOnlyList<ScriptDirectionOption> BuildDirectionOptions(bool buddyProvided)
        {
            return DbScriptFlagsCodec.AllowedDirections(buddyProvided)
                .Select(d => new ScriptDirectionOption(d, $"{KindLabel(d.Source)} ▶ {KindLabel(d.Target)}"))
                .ToList();
        }

        private string KindLabel(SourceTargetKind kind) => kind switch
        {
            SourceTargetKind.OriginalSource => step.TypeInfo.SourceLabel,
            SourceTargetKind.OriginalTarget => step.TypeInfo.TargetLabel,
            _ => "Buddy",
        };

        private static string BuildRawFlagsText(DecodedFlags decoded)
        {
            DbScriptFlagsCodec.TryEncode(decoded, out var flags, out _, out _);
            var text = $"data_flags = 0x{flags:X} ({flags})";
            if (decoded.UnmodeledBits != 0)
                text += $"  • includes unmodeled bits 0x{decoded.UnmodeledBits:X}";
            return text;
        }

        private void RaiseAll()
        {
            RaisePropertyChanged(nameof(SelectedDirection));
            RaisePropertyChanged(nameof(SelectedBuddyMode));
            RaisePropertyChanged(nameof(BuddyEntry));
            RaisePropertyChanged(nameof(BuddySearchValue));
            RaisePropertyChanged(nameof(IsGameObjectBuddy));
            RaisePropertyChanged(nameof(IncludeDespawned));
            RaisePropertyChanged(nameof(AllEligible));
            RaisePropertyChanged(nameof(CommandAdditional));
            RaisePropertyChanged(nameof(ShowBuddyFields));
            RaisePropertyChanged(nameof(ShowEntry));
            RaisePropertyChanged(nameof(ShowSearchValue));
            RaisePropertyChanged(nameof(ShowGoToggle));
            RaisePropertyChanged(nameof(EntryLabel));
            RaisePropertyChanged(nameof(SearchValueLabel));
            RaisePropertyChanged(nameof(SupportsAdditional));
            RaisePropertyChanged(nameof(HasWarnings));
            RaisePropertyChanged(nameof(SourceTargetSummary));
        }

        // exposed for the readable one-line preview in the step header
        public string SourceTargetSummary
        {
            get
            {
                var d = selectedDirection?.Direction;
                if (d == null)
                    return "";
                return $"{KindLabel(d.Value.Source)} ▶ {KindLabel(d.Value.Target)}";
            }
        }

        public static string FormatCulture(long v) => v.ToString(CultureInfo.InvariantCulture);
    }
}
