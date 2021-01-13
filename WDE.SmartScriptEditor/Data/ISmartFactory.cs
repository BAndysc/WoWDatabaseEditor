using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Data
{
    public interface ISmartFactory
    {
        SmartEvent EventFactory(int id);

        SmartEvent EventFactory(ISmartScriptLine line);

        SmartAction ActionFactory(int id, SmartSource source, SmartTarget target);

        SmartAction ActionFactory(ISmartScriptLine line);

        SmartSource SourceFactory(int id);

        SmartTarget TargetFactory(int id);
        
        SmartCondition ConditionFactory(int id);
        
        SmartCondition ConditionFactory(IConditionLine id);
    }
}