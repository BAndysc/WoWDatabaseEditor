using System;
using SmartFormat;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Parsing;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartSource : SmartBaseElement
    {
        public static readonly int SmartSourceParametersCount = 3;

        private ParameterValueHolder<long> condition;

        public SmartSource(int id) : base(SmartSourceParametersCount, id)
        {
            condition = new ParameterValueHolder<long>("Condition ID", Parameter.Instance);
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
                            pram1 = GetParameter(0).ToString(),
                            pram2 = GetParameter(1).ToString(),
                            pram3 = GetParameter(2).ToString(),
                            pram1value = GetParameter(0).Value,
                            pram2value = GetParameter(1).Value,
                            pram3value = GetParameter(2).Value,
                            x = 0.ToString(),
                            y = 0.ToString(),
                            z = 0.ToString(),
                            o = 0.ToString(),
                            stored = "Stored target #" + GetParameter(0).Value,
                            storedPoint = "Stored point #" + GetParameter(0).Value
                        });
                    return output;
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
        
        public SmartSource Copy()
        {
            SmartSource se = new(Id) {ReadableHint = ReadableHint, DescriptionRules = DescriptionRules};
            for (var i = 0; i < SmartSourceParametersCount; ++i)
            {
                se.GetParameter(i).Copy(GetParameter(i));
            }
            return se;
        }
    }
}