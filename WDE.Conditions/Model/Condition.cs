using WDE.Common.Database;
using WDE.Conditions.Data;
using Prism.Mvvm;

namespace WDE.Conditions.Model
{
    public class Condition : BindableBase
    {
        public static readonly int CONDITION_SOURCE_SMART_EVENT = 22;
        private readonly IConditionDataManager conditionDataManager;

        public int ElseGroup { get; set; }

        public int ConditionType { get; set; }

        public int ConditionTarget { get; set; }

        public int ConditionValue1 { get; set; }

        public int ConditionValue2 { get; set; }

        public int ConditionValue3 { get; set; }

        public int NegativeCondition { get; set; }

        public string Comment { get; set; }

        public Condition(IConditionDataManager conditionDataManager)
        {
            this.conditionDataManager = conditionDataManager;
        }

        public Condition(IConditionLine line, IConditionDataManager conditionDataManager)
        {
            ElseGroup = line.ElseGroup;
            ConditionType = line.ConditionType;
            ConditionTarget = line.ConditionTarget;
            ConditionValue1 = line.ConditionValue1;
            ConditionValue2 = line.ConditionValue2;
            ConditionValue3 = line.ConditionValue3;
            NegativeCondition = line.NegativeCondition;
            Comment = line.Comment;

            this.conditionDataManager = conditionDataManager;
        }

        public int Group
        {
            get { return ElseGroup; }
            set
            {
                ElseGroup = value;
                RaisePropertyChanged("Group");
            }
        }

        public int Type
        {
            get { return Type; }
            set
            {
                ConditionType = value;
                RaisePropertyChanged("Type");
                RaisePropertyChanged("ReadableType");
            }
        }
        
        public string ReadableType
        {
            get { return conditionDataManager.GetConditionData(ConditionType).Name; }
        }

        public int Target
        {
            get { return ConditionTarget; }
            set
            {
                ConditionTarget = value;
                RaisePropertyChanged("ReadableTarget");
            }
        }

        public string ReadableTarget
        {
            get { return ConditionTarget > 0 ? "Object" : "Invoker"; }
        }

        public int ConditionValueOne
        {
            get { return ConditionValue1; }
            set
            {
                ConditionValue1 = value;
                RaisePropertyChanged("ConditionValueOne");
            }
        }

        public int ConditionValueTwo
        {
            get { return ConditionValue2; }
            set
            {
                ConditionValue2 = value;
                RaisePropertyChanged("ConditionValueTwo");
            }
        }

        public int ConditionValueThree
        {
            get { return ConditionValue3; }
            set
            {
                ConditionValue3 = value;
                RaisePropertyChanged("ConditionValueThree");
            }
        }

        public bool IsNegative
        {
            get { return NegativeCondition > 0; }
            set
            {
                NegativeCondition = value ? 1 : 0;
                RaisePropertyChanged("IsNegative");
            }
        }

        
    }
}
