using System;
using System.Collections.Generic;
using SmartFormat;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Models
{
    public abstract class ParameterConditional
    {
        public Parameter CompareTo { get; set; }
        public Parameter Compared { get; set; }
        public WarningType WarningType { get; set; }
        public string Description { get; set; }

        protected ParameterConditional(WarningType warningType, string description)
        {
            WarningType = warningType;
            Description = description;
        }

        protected ParameterConditional(Parameter compared, WarningType warningType)
        {
            Compared = compared;
            WarningType = warningType;
        }

        protected ParameterConditional(Parameter compared, Parameter compareTo, WarningType warningType, string description = null)
        {
            Compared = compared;
            CompareTo = compareTo;
            WarningType = warningType;
            Description = description;
        }

        protected ParameterConditional(Parameter compared, int value, WarningType warningType, string description = null) : this(compared, new Parameter(value.ToString()), warningType, description)
        {
        }

        public void SetCompareTo(Parameter parameter)
        {
            CompareTo = parameter;
        }

        public void SetCompareTo(int parametr)
        {
            SetCompareTo(new Parameter(parametr.ToString()));
        }

        public void SetDescription(string error)
        {
            Description = Smart.Format(error, new { compared = Compared.Name, compareto = CompareTo.Name });
        }

        public abstract bool Validate();

        public static ParameterConditional Factory(string type, Parameter parameter = null)
        {
            switch (type)
            {
                case "CompareValue":
                    return new ParameterConditionalCompareValue(parameter);
            }
            return null;
        }
    }

    public class ParameterConditionalCompareAny : ParameterConditional
    {
        private readonly CompareType _compareType;
        private int _value;
        private readonly string _compareTo;
        private readonly string _compare;

        public ParameterConditionalCompareAny(SmartConditionalJsonData data) : base(null, WarningType.NOT_SET)
        {
            _compareType = data.CompareType;
            _value = data.CompareToValue;
            _compareTo = data.ComparedToAnyParam;
            _compare = data.ComparedAnyParam;
        }

        public bool Validate(int compare, int compareTo)
        {
            switch (_compareType)
            {
                case CompareType.EQUALS:
                    return compare == compareTo;
                case CompareType.NOT_EQUALS:
                    return compare != compareTo;
                case CompareType.LOWER_THAN:
                    return compare < compareTo;
                case CompareType.GREATER_THAN:
                    return compare > compareTo;
                case CompareType.LOWER_OR_EQUALS:
                    return compare <= compareTo;
                case CompareType.GREATER_OR_EQUALS:
                    return compare >= compareTo;
            }
            return false;
        }

        internal bool Validate(DescriptionParams paramz)
        {
            int compareSource = ValueForSource(_compare, paramz);
            if (_compareTo != null)
                _value = ValueForSource(_compareTo, paramz);
            return Validate(compareSource, _value);
        }

        private int ValueForSource(string compare, DescriptionParams paramz)
        {
            int compareSource = 0;
            switch (compare)
            {
                case "sourcetype":
                    compareSource = paramz.source_type;
                    break;
                case "pram1":
                    compareSource = paramz.pram1;
                    break;
                case "pram2":
                    compareSource = paramz.pram2;
                    break;
                case "pram3":
                    compareSource = paramz.pram3;
                    break;
                case "pram4":
                    compareSource = paramz.pram4;
                    break;
                case "pram5":
                    compareSource = paramz.pram5;
                    break;
                case "pram6":
                    compareSource = paramz.pram6;
                    break;
            }
            return compareSource;
        }

        public override bool Validate()
        {
            return false;
        }
    }

    public class ParameterConditionalCompareValue : ParameterConditional
    {
        private CompareType compareType;
        private int max;

        public ParameterConditionalCompareValue(Parameter compared) : base(compared, WarningType.INVALID_VALUE) { }

        public ParameterConditionalCompareValue(Parameter compared, Parameter compareTo, CompareType compareType)
            : base(compared, compareTo, WarningType.INVALID_PARAMETER)
        {
            this.compareType = compareType;
            Description = compared.Name + " must be " + compareType + " " + compareTo.Name;
        }

        public ParameterConditionalCompareValue(Parameter compared, int compareTo, CompareType compareType)
            : base(compared, compareTo, WarningType.INVALID_PARAMETER)
        {
            this.compareType = compareType;
            Description = compared.Name + " must be " + compareType + " " + compareTo;
        }

        public ParameterConditionalCompareValue(Parameter compared, int compareTo, CompareType compareType, string description)
            : base(compared, compareTo, WarningType.INVALID_PARAMETER)
        {
            this.compareType = compareType;
            Description = description;
        }

        public ParameterConditionalCompareValue(Parameter compared, int min, int max)
            : base(compared, min, WarningType.INVALID_PARAMETER)
        {
            compareType = CompareType.BETWEEN;
            this.max = max;
            Description = compared.Name + " must be between " + min + " and " + max;
        }


        public void SetCompareType(CompareType compareType)
        {
            this.compareType = compareType;
            Description = Compared.Name + " must be " + compareType + " " + CompareTo.Name;
        }

        public override bool Validate()
        {
            switch (compareType)
            {
                case CompareType.EQUALS:
                    return Compared.GetValue() == CompareTo.GetValue();
                case CompareType.NOT_EQUALS:
                    return Compared.GetValue() != CompareTo.GetValue();
                case CompareType.LOWER_THAN:
                    return Compared.GetValue() < CompareTo.GetValue();
                case CompareType.GREATER_THAN:
                    return Compared.GetValue() > CompareTo.GetValue();
                case CompareType.LOWER_OR_EQUALS:
                    return Compared.GetValue() <= CompareTo.GetValue();
                case CompareType.GREATER_OR_EQUALS:
                    return Compared.GetValue() >= CompareTo.GetValue();
                case CompareType.BETWEEN:
                    return Compared.GetValue() >= CompareTo.GetValue() && Compared.GetValue() <= max;
            }
            return false;
        }
    }

    public class ParameterConditionalFlag : ParameterConditional
    {
        private List<int> flags;

        public ParameterConditionalFlag(Parameter compared, List<int> flags) : base(compared, WarningType.INVALID_VALUE)
        {
            this.flags = flags;
        }

        public override bool Validate()
        {
            int value = Compared.GetValue();
            foreach (int flag in flags)
                value = value & ~flag;
            if (value > 0)
            {
                List<int> unsupported_flags = new List<int>();
                string bits = Convert.ToString(value, 2);
                for (int i = 0; i < bits.Length; ++i)
                {
                    if (bits[i] == '1')
                    {
                        unsupported_flags.Add((int)Math.Pow(2, (bits.Length - i - 1)));
                    }
                }
                Description = Compared.Name + " contains unsupported flags: " + String.Join(", ", unsupported_flags);
                return false;
            }
            return true;
        }
    }

    public class ParameterConditionalGroup : ParameterConditional
    {
        private List<ParameterConditional> conditionals;
        public ParameterConditionalGroup(List<ParameterConditional> conditionals, string description)
            : base(WarningType.INVALID_PARAMETER, description)
        {
            this.conditionals = conditionals;
        }
        public override bool Validate()
        {
            foreach (ParameterConditional conditional in conditionals)
            {
                if (!conditional.Validate())
                    return false;
            }
            return true;
        }
    }

    public class ParameterConditionalInversed : ParameterConditional
    {
        private ParameterConditional conditional;
        public ParameterConditionalInversed(ParameterConditional conditional) : base(conditional.Compared, conditional.CompareTo, conditional.WarningType, conditional.Description)
        {
            this.conditional = conditional;
        }

        public override bool Validate()
        {
            return !conditional.Validate();
        }
    }
}
