using System.Collections.Generic;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using SmartFormat;
using WDE.CMangosConditions.Data;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Parameters;
using WDE.Parameters.Models;

namespace WDE.CMangosConditions.ViewModels
{
    /// <summary>DI-free string parameter (StringParameter.Instance needs a live container).</summary>
    internal class PlainStringParameter : GenericBaseParameter<string>
    {
        public static PlainStringParameter Instance { get; } = new();
        public override string ToString(string value) => value;
    }

    public class MangosConditionViewModel : BindableBase
    {
        public const int ParametersCount = 4;

        private readonly ParameterValueHolder<long>[] parameters = new ParameterValueHolder<long>[ParametersCount];
        private int conditionType;
        private string? readableHint;
        private string? negativeReadableHint;
        private uint originalEntry;
        private bool isExpanded = true;
        private bool isSelected;
        private MangosConditionSourceTarget? sourceTarget;
        private string defaultTargetName = "target";

        public event System.Action<MangosConditionViewModel, int, int>? ConditionChanged;

        public MangosConditionViewModel()
        {
            for (int i = 0; i < ParametersCount; ++i)
            {
                parameters[i] = new ParameterValueHolder<long>(Parameter.Instance, 0);
                parameters[i].OnValueChanged += (_, _, _) => RaisePropertyChanged(nameof(Readable));
            }

            Negate = new ParameterValueHolder<long>("Negate result (flag 0x1)", BoolParameter.Instance, 0);
            SwapTargets = new ParameterValueHolder<long>("Checked on", MakeCheckOnParameter(null), 0);
            Comment = new ParameterValueHolder<string>("Comment", PlainStringParameter.Instance, "");

            Negate.OnValueChanged += (_, _, _) => RaisePropertyChanged(nameof(Readable));
            SwapTargets.OnValueChanged += (_, _, _) => RaisePropertyChanged(nameof(Readable));
        }

        /// <summary>
        /// Sets what the core will pass as this condition's source/target objects: the
        /// "Checked on" combo lists the actual actors and the readable text names them.
        /// null = unknown caller, generic "source"/"target" wording.
        /// </summary>
        public void SetSourceTarget(MangosConditionSourceTarget? context)
        {
            sourceTarget = context;
            SwapTargets.Parameter = MakeCheckOnParameter(context);
            RaisePropertyChanged(nameof(Readable));
        }

        private static IParameter<long> MakeCheckOnParameter(MangosConditionSourceTarget? context)
        {
            var target = context?.Target ?? "target";
            var source = context == null ? "source" : context.Source ?? "(nothing — no source object is passed here)";
            return new Parameter
            {
                Items = new Dictionary<long, SelectOption>
                {
                    [0] = new(target, "Default: the condition is checked on the target object."),
                    [1] = new(source, "Source and target are swapped before the check (flag 0x2)."),
                }
            };
        }

        /// <summary>condition_entry this node was loaded with; 0 = created in the editor.</summary>
        public uint OriginalEntry
        {
            get => originalEntry;
            set
            {
                originalEntry = value;
                RaisePropertyChanged(nameof(Readable));
            }
        }

        public int ConditionType => conditionType;

        /// <summary>Logical nodes (AND/OR/NOT) have MaxChildren > 0; leaves have 0.</summary>
        public int MinChildren { get; private set; }
        public int MaxChildren { get; private set; }
        public bool IsLogical => MaxChildren > 0;

        public ParameterValueHolder<long> Value1 => parameters[0];
        public ParameterValueHolder<long> Value2 => parameters[1];
        public ParameterValueHolder<long> Value3 => parameters[2];
        public ParameterValueHolder<long> Value4 => parameters[3];
        public ParameterValueHolder<long> Negate { get; }
        public ParameterValueHolder<long> SwapTargets { get; }
        public ParameterValueHolder<string> Comment { get; }

        public ObservableCollection<MangosConditionViewModel> Children { get; } = new();
        public MangosConditionViewModel? Parent { get; set; }

        public bool IsExpanded
        {
            get => isExpanded;
            set => SetProperty(ref isExpanded, value);
        }

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public ParameterValueHolder<long> GetParameter(int i) => parameters[i];

        public uint Flags
        {
            get
            {
                uint flags = 0;
                if (Negate.Value != 0)
                    flags |= 1;
                if (SwapTargets.Value != 0)
                    flags |= 2;
                return flags;
            }
        }

        public string Readable => GetReadable(withTags: true, withEntry: true);

        public string GetReadable(bool withTags, bool withEntry)
        {
            string? readable = readableHint;
            if (readable == null)
                return "(unknown condition " + conditionType + ")" + (withEntry ? EntrySuffix : "");
            if (negativeReadableHint != null && Negate.Value != 0)
                readable = negativeReadableHint;
            var open = withTags ? "[p]" : "";
            var close = withTags ? "[/p]" : "";

            // {target}/{source} name the post-swap slots the core evaluates against;
            // {sourceortarget} follows the core's source-if-provided-else-target searcher
            // (CONDITION_AREAID and friends)
            bool swapped = SwapTargets.Value != 0;
            string targetName, sourceName, sourceOrTargetName;
            if (sourceTarget != null)
            {
                targetName = swapped ? sourceTarget.Source ?? "(nothing)" : sourceTarget.Target;
                sourceName = swapped ? sourceTarget.Target : sourceTarget.Source ?? "(nothing)";
                sourceOrTargetName = swapped || sourceTarget.Source != null ? sourceName : targetName;
            }
            else
            {
                targetName = swapped ? "source" : defaultTargetName;
                sourceName = swapped ? "target" : "source";
                sourceOrTargetName = swapped ? "source" : defaultTargetName;
            }

            var formatted = Smart.Format(readable, new
            {
                negate = Negate.Value == 0,
                entry = originalEntry,
                target = targetName,
                source = sourceName,
                sourceortarget = sourceOrTargetName,
                pram1 = open + GetParameter(0) + close,
                pram2 = open + GetParameter(1) + close,
                pram3 = open + GetParameter(2) + close,
                pram4 = open + GetParameter(3) + close,
                pram1value = GetParameter(0).Value,
                pram2value = GetParameter(1).Value,
                pram3value = GetParameter(2).Value,
                pram4value = GetParameter(3).Value,
            });
            // templates that don't name an actor keep the explicit swap marker
            if (swapped && !UsesActorPlaceholders(readable))
                formatted = "(source↔target) " + formatted;
            return withEntry ? formatted + EntrySuffix : formatted;
        }

        private static bool UsesActorPlaceholders(string template) =>
            template.Contains("{target}") || template.Contains("{source}") || template.Contains("{sourceortarget}");

        private string EntrySuffix => originalEntry != 0 ? $"  [s]#{originalEntry}[/s]" : "";

        internal void UpdateCondition(MangosConditionJson data)
        {
            var old = conditionType;
            conditionType = data.Id;
            readableHint = data.Description;
            negativeReadableHint = data.NegativeDescription;
            MinChildren = data.MinChildren;
            MaxChildren = data.MaxChildren;
            // fallback {target} wording when no caller context is known, from the type's subject
            defaultTargetName = data.Subject.DefaultTargetName();
            RaisePropertyChanged(nameof(Readable));
            RaisePropertyChanged(nameof(IsLogical));
            ConditionChanged?.Invoke(this, old, conditionType);
        }

        /// <summary>
        /// This node as a raw line, without child references (the codec assigns
        /// entries and fills Value1..4 of logical nodes from the tree structure).
        /// </summary>
        public AbstractMangosConditionLine ToLine()
        {
            return new AbstractMangosConditionLine
            {
                ConditionEntry = originalEntry,
                ConditionType = conditionType,
                Value1 = IsLogical ? 0 : (uint)Value1.Value,
                Value2 = IsLogical ? 0 : (uint)Value2.Value,
                Value3 = IsLogical ? 0 : (uint)Value3.Value,
                Value4 = IsLogical ? 0 : (uint)Value4.Value,
                Flags = Flags,
                Comments = EffectiveComment,
            };
        }

        public string EffectiveComment =>
            string.IsNullOrWhiteSpace(Comment.Value) ? Readable.RemoveTags() : Comment.Value;

        public IEnumerable<ParameterValueHolder<long>> Values()
        {
            for (int i = 0; i < ParametersCount; ++i)
                yield return parameters[i];
            yield return Negate;
            yield return SwapTargets;
        }

        public IEnumerable<ParameterValueHolder<string>> StringValues()
        {
            yield return Comment;
        }

        /// <summary>Depth-first walk over this node and all descendants.</summary>
        public IEnumerable<MangosConditionViewModel> Descendants()
        {
            yield return this;
            foreach (var child in Children)
                foreach (var d in child.Descendants())
                    yield return d;
        }
    }
}
