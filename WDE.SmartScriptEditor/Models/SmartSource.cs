using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SmartFormat;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Parsing;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Editor;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartSource : SmartBaseElement
    {
        protected readonly IEditorFeatures features;
        public event Action<SmartSource, IList<ICondition>?, IList<ICondition>?> OnConditionsChanged = delegate { };
        
        protected SmartAction? parent;
        
        private ParameterValueHolder<long> condition;

        public readonly ParameterValueHolder<float>[]? Position;

        public SmartSource(int id, IEditorFeatures features, bool? withPosition = null) : base(id,
            features.TargetParametersCount,
            that => new SmartScriptParameterValueHolder(Parameter.Instance, 0, that))
        {
            this.features = features;
            condition = new ParameterValueHolder<long>("Condition ID", Parameter.Instance, 0);

            if (withPosition.HasValue && withPosition.Value ||
                (!withPosition.HasValue && features.SourceHasPosition))
            {
                Position = new ParameterValueHolder<float>[4];

                Position[0] = new ParameterValueHolder<float>("Target X", FloatParameter.Instance, 0);
                Position[1] = new ParameterValueHolder<float>("Target Y", FloatParameter.Instance, 0);
                Position[2] = new ParameterValueHolder<float>("Target Z", FloatParameter.Instance, 0);
                Position[3] = new ParameterValueHolder<float>("Target O", FloatParameter.Instance, 0);

                for (var i = 0; i < 4; ++i)
                    Position[i].PropertyChanged += (_, _) => CallOnChanged();   
            }
        }

        public bool IsPosition { get; set; }

        public SmartAction? Parent
        {
            get => parent;
            set => parent = value;
        }

        public override int LineId
        {
            get => parent?.LineId ?? -1;
            set { }
        }
        
        public float X
        {
            get => Position![0].Value;
            set => Position![0].Value = value;
        }

        public float Y
        {
            get => Position![1].Value;
            set => Position![1].Value = value;
        }

        public float Z
        {
            get => Position![2].Value;
            set => Position![2].Value = value;
        }

        public float O
        {
            get => Position![3].Value;
            set => Position![3].Value = value;
        }

        private IList<ICondition>? conditions;
        public IList<ICondition>? Conditions
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
                parent?.InvalidateReadable();
            }
        }

        public ParameterValueHolder<long> Condition => condition;

        public override string Readable
        {
            get
            {
                try
                {
                    string output = Smart.Format(ReadableHint,
                        new
                        {
                            pram1 = GetParameter(0),
                            pram2 = GetParameter(1),
                            pram3 = GetParameter(2),
                            pram1value = GetParameter(0).Value,
                            pram2value = GetParameter(1).Value,
                            pram3value = GetParameter(2).Value,
                            x = X.ToString(CultureInfo.InvariantCulture),
                            y = Y.ToString(CultureInfo.InvariantCulture),
                            z = Z.ToString(CultureInfo.InvariantCulture),
                            o = O.ToString(CultureInfo.InvariantCulture),
                            invoker = GetInvokerNameWithContext()
                        });
                    if ((Conditions?.Count ?? 0) == 0)
                        return output;
                    return $"{output} (+)";
                }
                catch (ParsingErrors e)
                {
                    Console.WriteLine(e.ToString());
                    return $"Source {Id} has invalid Readable format in targets.json";
                }
                catch (FormattingException e)
                {
                    Console.WriteLine(e.ToString());
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

                return parentEventData.Value.Invoker.Name;
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
            if (Position == null)
                return false;
            
            for (var i = 0; i < 4; ++i)
            {
                if (Position[i].Value != 0)
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
            SmartSource se = new(Id, features, Position != null)
            {
                ReadableHint = ReadableHint, 
                DescriptionRules = DescriptionRules,
                IsPosition = IsPosition
            };
            se.CopyParameters(this);

            if (Position != null)
            {
                for (var i = 0; i < Position.Length; ++i)
                    se.Position![i].Copy(Position[i]);
            }

            se.Conditions = Conditions?.ToList();
            return se;
        }
    }
}