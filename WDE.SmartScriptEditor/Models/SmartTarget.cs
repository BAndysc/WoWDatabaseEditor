using System;
using System.Globalization;
using System.Linq;
using SmartFormat;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Parsing;
using WDE.Common.Parameters;
using WDE.Parameters.Models;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartTarget : SmartSource
    {
        public readonly ParameterValueHolder<float>[] Position;

        public SmartTarget(int id) : base(id)
        {
            Position = new ParameterValueHolder<float>[4];

            Position[0] = new ParameterValueHolder<float>("Target X", FloatParameter.Instance, 0);
            Position[1] = new ParameterValueHolder<float>("Target Y", FloatParameter.Instance, 0);
            Position[2] = new ParameterValueHolder<float>("Target Z", FloatParameter.Instance, 0);
            Position[3] = new ParameterValueHolder<float>("Target O", FloatParameter.Instance, 0);

            for (var i = 0; i < 4; ++i)
                Position[i].PropertyChanged += (_, _) => CallOnChanged();
        }

        public bool IsPosition { get; set; }

        public float X
        {
            get => Position[0].Value;
            set => Position[0].Value = value;
        }

        public float Y
        {
            get => Position[1].Value;
            set => Position[1].Value = value;
        }

        public float Z
        {
            get => Position[2].Value;
            set => Position[2].Value = value;
        }

        public float O
        {
            get => Position[3].Value;
            set => Position[3].Value = value;
        }

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
                    return $"Target {Id} has invalid Readable format in targets.json";
                }
                catch (FormattingException e)
                {
                    Console.WriteLine(e.ToString());
                    return $"Target {Id} has invalid Readable format in targets.json";
                }
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

        public new SmartTarget Copy()
        {
            SmartTarget se = new(Id)
            {
                ReadableHint = ReadableHint,
                DescriptionRules = DescriptionRules,
                IsPosition = IsPosition
            };
            
            for (var i = 0; i < SmartSourceParametersCount; ++i)
                se.GetParameter(i).Copy(GetParameter(i));
            
            for (var i = 0; i < Position.Length; ++i)
                se.Position[i].Copy(Position[i]);
            
            se.Conditions = Conditions?.ToList();
            
            return se;
        }
    }
}