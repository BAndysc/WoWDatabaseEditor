using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Conditions.Data;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class SmartFactory : ISmartFactory
    {
        private readonly IParameterFactory parameterFactory;
        private readonly ISmartDataManager smartDataManager;
        private readonly IConditionDataManager conditionDataManager;

        public SmartFactory(IParameterFactory parameterFactory, 
            ISmartDataManager smartDataManager, 
            IConditionDataManager conditionDataManager)
        {
            this.parameterFactory = parameterFactory;
            this.smartDataManager = smartDataManager;
            this.conditionDataManager = conditionDataManager;
        }

        public SmartEvent EventFactory(int id)
        {
            if (!smartDataManager.Contains(SmartType.SmartEvent, id))
                throw new NullReferenceException("No data for event id " + id);

            SmartEvent ev = new(id);
            SmartGenericJsonData raw = smartDataManager.GetRawData(SmartType.SmartEvent, id);
            ev.Chance.Value = 100;
            SetParameterObjects(ev, raw);

            if (raw.DescriptionRules != null)
            {
                ev.DescriptionRules = new List<DescriptionRule>();
                foreach (SmartDescriptionRulesJsonData rule in raw.DescriptionRules)
                    ev.DescriptionRules.Add(new DescriptionRule(rule));
            }

            return ev;
        }

        public SmartEvent EventFactory(ISmartScriptLine line)
        {
            SmartEvent ev = EventFactory(line.EventType);

            ev.Chance.Value = line.EventChance;
            ev.Phases.Value = line.EventPhaseMask;
            ev.Flags.Value = line.EventFlags;
            ev.CooldownMin.Value = line.EventCooldownMin;
            ev.CooldownMax.Value = line.EventCooldownMax;

            for (var i = 0; i < SmartEvent.SmartEventParamsCount; ++i)
            {
                ev.GetParameter(i).Value = line.GetEventParam(i);
            }

            return ev;
        }

        public SmartCondition ConditionFactory(int id)
        {
            if (!conditionDataManager.HasConditionData(id))
                throw new NullReferenceException("No data for condition id " + id);

            SmartCondition ev = new(id);
            var raw = conditionDataManager.GetConditionData(id);
            SetParameterObjects(ev, raw);

            return ev;
        }
        
        public SmartCondition ConditionFactory(IConditionLine line)
        {
            SmartCondition condition = ConditionFactory(line.ConditionType);

            condition.Inverted.Value = line.NegativeCondition;
            condition.ConditionTarget.Value = line.ConditionTarget;
            condition.GetParameter(0).Value = line.ConditionValue1;
            condition.GetParameter(1).Value = line.ConditionValue2;
            condition.GetParameter(2).Value = line.ConditionValue3;

            return condition;
        }
        
        public SmartAction ActionFactory(int id, SmartSource source, SmartTarget target)
        {
            if (!smartDataManager.Contains(SmartType.SmartAction, id))
                throw new NullReferenceException("No data for action id " + id);

            SmartAction action = new(id, source, target);

            SetParameterObjects(action, smartDataManager.GetRawData(SmartType.SmartAction, id));

            return action;
        }

        public SmartAction ActionFactory(ISmartScriptLine line)
        {
            SmartSource source = SourceFactory(line);
            SmartTarget target = TargetFactory(line);

            SmartAction action = ActionFactory(line.ActionType, source, target);

            for (var i = 0; i < SmartAction.SmartActionParametersCount; ++i)
                action.GetParameter(i).Value = line.GetActionParam(i);

            return action;
        }

        public SmartTarget TargetFactory(int id)
        {
            if (!smartDataManager.Contains(SmartType.SmartTarget, id))
                throw new NullReferenceException("No data for target id " + id);

            SmartTarget target = new(id);

            SetParameterObjects(target, smartDataManager.GetRawData(SmartType.SmartTarget, id));

            var targetTypes = smartDataManager.GetRawData(SmartType.SmartTarget, id).Types;

            if (targetTypes != null && targetTypes.Contains("Position"))
                target.IsPosition = true;

            return target;
        }

        public SmartSource SourceFactory(int id)
        {
            if (!smartDataManager.Contains(SmartType.SmartSource, id))
                throw new NullReferenceException("No data for source id " + id);

            SmartSource source = new(id);

            SetParameterObjects(source, smartDataManager.GetRawData(SmartType.SmartSource, id));

            return source;
        }
        
        public SmartTarget TargetFactory(ISmartScriptLine line)
        {
            SmartTarget target = TargetFactory(line.TargetType);

            target.X = line.TargetX;
            target.Y = line.TargetY;
            target.Z = line.TargetZ;
            target.O = line.TargetO;

            target.Condition.Value = (line.TargetConditionId);

            for (var i = 0; i < SmartSource.SmartSourceParametersCount; ++i)
                target.GetParameter(i).Value = line.GetTargetParam(i);

            return target;
        }

        private int GetSourceParameter(ISmartScriptLine line, int i)
        {
            // ugly but DB is in such form
            switch (i)
            {
                case 0:
                    return line.SourceParam1;
                case 1:
                    return line.SourceParam2;
                case 2:
                    return line.SourceParam3;
            }

            throw new ArgumentException("Source parameter out of range");
        }

        private SmartSource SourceFactory(ISmartScriptLine line)
        {
            SmartSource source = SourceFactory(line.SourceType);

            source.Condition.Value = line.SourceConditionId;

            for (var i = 0; i < SmartSource.SmartSourceParametersCount; ++i)
                source.GetParameter(i).Value = line.GetSourceParam(i);

            return source;
        }

        private int GetTargetParameter(ISmartScriptLine line, int i)
        {
            // ugly but DB is in such form
            switch (i)
            {
                case 0:
                    return line.TargetParam1;
                case 1:
                    return line.TargetParam2;
                case 2:
                    return line.TargetParam3;
            }

            throw new ArgumentException("Target parameter out of range");
        }

        private void SetParameterObjects(SmartBaseElement element, SmartGenericJsonData data)
        {
            element.ReadableHint = data.Description;
            if (data.Parameters == null)
                return;

            for (var i = 0; i < data.Parameters.Count; ++i)
            {
                string key = data.Parameters[i].Type;
                if (data.Parameters[i].Values != null)
                {
                    key = $"{data.Name}_{i}";
                    if (!parameterFactory.IsRegistered(key))
                        parameterFactory.Register(key, new Parameter(){Items = data.Parameters[i].Values});
                }
                
                IParameter<int> parameter = parameterFactory.Factory(key);
                element.GetParameter(i).Name = data.Parameters[i].Name;
                element.GetParameter(i).Value = data.Parameters[i].DefaultVal;
                element.GetParameter(i).Parameter = parameter;
            }
        }
        
        private void SetParameterObjects(SmartBaseElement element, ConditionJsonData data)
        {
            element.ReadableHint = data.Description;
            if (data.Parameters == null)
                return;

            for (var i = 0; i < data.Parameters.Count; ++i)
            {
                string key = data.Parameters[i].Type;
                if (data.Parameters[i].Values != null)
                {
                    key = $"{data.Name}_{i}";
                    if (!parameterFactory.IsRegistered(key))
                        parameterFactory.Register(key, new Parameter(){Items = data.Parameters[i].Values});
                }
                
                IParameter<int> parameter = parameterFactory.Factory(key);

                element.GetParameter(i).Name = data.Parameters[i].Name;
                element.GetParameter(i).Parameter = parameter;
            }
        }
    }
}