using System.Linq;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartTarget : SmartSource
    {
        public SmartTarget(int id, IEditorFeatures features) : base(id, features)
        {
            isSource = false;
        }

        public new SmartTarget Copy()
        {
            SmartTarget se = new(Id, features)
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

        public override SmartType SmartType => SmartType.SmartTarget;
    }
}