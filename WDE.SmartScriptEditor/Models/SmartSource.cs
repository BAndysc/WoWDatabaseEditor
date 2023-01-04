using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SmartFormat;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Parsing;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Models.Helpers;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartSource : SmartBaseElement
    {
        protected readonly IEditorFeatures features;
        public event Action<SmartSource, IReadOnlyList<ICondition>?, IReadOnlyList<ICondition>?> OnConditionsChanged = delegate { };
        
        protected SmartAction? parent;
        
        private ParameterValueHolder<long> condition;

        protected bool isSource = true;

        public SmartSource(int id, IEditorFeatures features) : base(id,
            features.TargetParametersCount,
            that => new SmartScriptParameterValueHolder(Parameter.Instance, 0, that))
        {
            this.features = features;
            condition = new ParameterValueHolder<long>("Condition ID", Parameter.Instance, 0);
        }

        public bool IsPosition { get; set; }

        public SmartAction? Parent
        {
            get => parent;
            set => parent = value;
        }

        public override SmartScriptBase? Script => parent?.Script;

        public override SmartType SmartType => SmartType.SmartSource;

        public override int VirtualLineId
        {
            get => parent?.VirtualLineId ?? -1;
            set { }
        }
        
        public override int? DestinationEventId
        {
            get => parent?.DestinationEventId;
            set { }
        }

        public override int? SavedDatabaseEventId
        {
            get => parent?.SavedDatabaseEventId;
            set { }
        }

        public override int? DestinationTimedActionListId
        {
            get => parent?.DestinationTimedActionListId;
            set { }
        }

        public override int? SavedTimedActionListId
        {
            get => parent?.SavedTimedActionListId;
            set { }
        }

        public int? DestinationConditionId { get; set; }

        public float X
        {
            get => this.GetFloatValueOrDefault(0);
            set => GetFloatParameter(0).Value = value;
        }

        public float Y
        {
            get => this.GetFloatValueOrDefault(1);
            set => GetFloatParameter(1).Value = value;
        }

        public float Z
        {
            get => this.GetFloatValueOrDefault(2);
            set => GetFloatParameter(2).Value = value;
        }

        public float O
        {
            get => this.GetFloatValueOrDefault(3);
            set => GetFloatParameter(3).Value = value;
        }

        private IReadOnlyList<ICondition>? conditions;
        public IReadOnlyList<ICondition>? Conditions
        {
            get => conditions;
            set
            {
                if (value != null && value.Count == 0)
                    value = null;
                if (ReferenceEquals(conditions, value))
                    return;
                var old = conditions;
                conditions = value;
                OnConditionsChanged?.Invoke(this, old, value);
                InvalidateReadable();
                parent?.InvalidateReadable();
            }
        }

        public ParameterValueHolder<long> Condition => condition;

        protected override string ReadableImpl
        {
            get
            {
                if (ReadableHint == null)
                    return "(internal error 'Readable' is null, report at github)";
                
                try
                {
                    string output = Smart.Format(ReadableHint,
                        new
                        {
                            pram1 = this.GetTextOrDefault(0) ?? "",
                            pram2 = this.GetTextOrDefault(1) ?? "",
                            pram3 = this.GetTextOrDefault(2) ?? "",
                            pram1value = this.GetValueOrDefault(0),
                            pram2value = this.GetValueOrDefault(1),
                            pram3value = this.GetValueOrDefault(2),
                            x = X.ToString(CultureInfo.InvariantCulture),
                            y = Y.ToString(CultureInfo.InvariantCulture),
                            z = Z.ToString(CultureInfo.InvariantCulture),
                            o = O.ToString(CultureInfo.InvariantCulture),
                            fpram1 = X.ToString(CultureInfo.InvariantCulture),
                            fpram2 = Y.ToString(CultureInfo.InvariantCulture),
                            fpram3 = Z.ToString(CultureInfo.InvariantCulture),
                            fpram4 = O.ToString(CultureInfo.InvariantCulture),
                            fpram1value = X,
                            fpram2value = Y,
                            fpram3value = Z,
                            fpram4value = O,
                            invoker = GetInvokerNameWithContext(),
                            isSourcePosition = parent?.Source?.IsPosition,
                            isSource = isSource,
                            isTarget = !isSource,
                            
                        });
                    if ((Conditions?.Count ?? 0) == 0)
                        return output;
                    return $"{output} (+)";
                }
                catch (ParsingErrors e)
                {
                    LOG.LogWarning(e.ToString());
                    return $"Source {Id} has invalid Readable format in targets.json";
                }
                catch (FormattingException e)
                {
                    LOG.LogWarning(e.ToString());
                    return $"Source {Id} has invalid Readable format in targets.json";
                }
            }
        }
        
        protected string GetInvokerNameWithContext()
        {
            if (Parent?.Parent?.Parent == null)
                return "Last action invoker";

            try
            {
                var parentEventData = Parent.Parent.Parent.TryGetEventData(Parent.Parent);

                if (parentEventData?.Invoker == null)
                    return "Last action invoker";

                return parentEventData.Invoker.Name;
            }
            catch (Exception)
            {
                return "Last action invoker";
            }

        }
        
        public string GetCoordsXyz()
        {
            return $"({X.ToString(CultureInfo.InvariantCulture)}, {Y.ToString(CultureInfo.InvariantCulture)}, {Z.ToString(CultureInfo.InvariantCulture)})";
        }
        
        public string GetCoords()
        {
            return $"({X.ToString(CultureInfo.InvariantCulture)}, {Y.ToString(CultureInfo.InvariantCulture)}, {Z.ToString(CultureInfo.InvariantCulture)}, {O.ToString(CultureInfo.InvariantCulture)})";
        }

        private bool HasPosition()
        {
            for (var i = 0; i < FloatParametersCount; ++i)
            {
                if (GetFloatParameter(i).Value != 0)
                    return true;
            }

            return false;
        }

        public string GetPosition()
        {
            string output = Readable;
            return output.Contains("position") || IsPosition
                ? output
                : "position of " + output + (HasPosition() ? " moved by offset " + GetCoordsXyz() : "");
        }
        
        public SmartSource Copy()
        {
            SmartSource se = new(Id, features)
            {
                ReadableHint = ReadableHint, 
                DescriptionRules = DescriptionRules,
                IsPosition = IsPosition
            };
            se.CopyParameters(this);

            se.Condition.Value = Condition.Value;
            se.Conditions = Conditions?.ToList();
            return se;
        }
    }
}