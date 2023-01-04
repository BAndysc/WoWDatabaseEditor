using System;
using WDE.Common;
using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Data
{
    public interface ISmartFactory
    {
        SmartEvent EventFactory(SmartScriptBase? parent, int id);

        SmartEvent EventFactory(ISmartScriptLine line);

        SmartAction ActionFactory(int id, SmartSource? source, SmartTarget? target);

        SmartAction ActionFactory(ISmartScriptLine line);

        SmartSource SourceFactory(int id);

        SmartTarget TargetFactory(int id);
        
        SmartCondition ConditionFactory(int id);
        
        SmartCondition ConditionFactory(ICondition id);
        
        void UpdateEvent(SmartEvent smartEvent, int id);

        void UpdateAction(SmartAction smartAction, int id);
        
        void UpdateCondition(SmartCondition smartCondition, int id);
        
        void UpdateSource(SmartSource smartSource, int id);
        
        void UpdateTarget(SmartTarget smartTarget, int id);
    }

    public static class SmartFactoryExtensions
    {
        public static void SafeUpdateEvent(this ISmartFactory factory, SmartEvent smart, int id)
        {
            try 
            {
                factory.UpdateEvent(smart, id);
            } 
            catch (Exception ex)
            {
                LOG.LogWarning(ex);
            }
        }
        
        public static void SafeUpdateAction(this ISmartFactory factory, SmartAction smart, int id)
        {
            try 
            {
                factory.UpdateAction(smart, id);
            } 
            catch (Exception ex)
            {
                LOG.LogWarning(ex);
            }
        }
        
        public static void SafeUpdateCondition(this ISmartFactory factory, SmartCondition smart, int id)
        {
            try 
            {
                factory.UpdateCondition(smart, id);
            } 
            catch (Exception ex)
            {
                LOG.LogWarning(ex);
            }
        }
        
        public static void SafeUpdateSource(this ISmartFactory factory, SmartSource smart, int id)
        {
            try 
            {
                factory.UpdateSource(smart, id);
            } 
            catch (Exception ex)
            {
                LOG.LogWarning(ex);
            }
        }
        
        public static void SafeUpdateTarget(this ISmartFactory factory, SmartTarget smart, int id)
        {
            try 
            {
                factory.UpdateTarget(smart, id);
            } 
            catch (Exception ex)
            {
                LOG.LogWarning(ex);
            }
        }
    }

    public class InvalidSmartSourceException : Exception
    {
        public InvalidSmartSourceException(int id) : base("Invalid source with id " + id) { }
    }

    public class InvalidSmartTargetException : Exception
    {
        public InvalidSmartTargetException(int id) : base("Invalid target with id " + id) { }
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