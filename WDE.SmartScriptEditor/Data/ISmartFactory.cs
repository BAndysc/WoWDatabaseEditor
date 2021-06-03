using System;
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
        
        void UpdateEvent(SmartEvent smartEvent, int id);

        void UpdateAction(SmartAction smartAction, int id);
        
        void UpdateCondition(SmartCondition smartCondition, int id);
        
        void UpdateSource(SmartSource smartSource, int id);
        
        void UpdateTarget(SmartTarget smartTarget, int id);
    }

    public class InvalidSmartSourceException : Exception
    {
        public InvalidSmartSourceException(int id) : base("Invalid source with id " + id) { }
    }

    public class InvalidSmartTargetException : Exception
    {
        public InvalidSmartTargetException(int id) : base("Invalid event with id " + id) { }
    }

    public class InvalidSmartActionException : Exception
    {
        public InvalidSmartActionException(int id) : base("Invalid action with id " + id) { }
    }

    public class InvalidSmartEventException : Exception
    {
        public InvalidSmartEventException(int id) : base("Invalid event with id " + id) { }
    }
}