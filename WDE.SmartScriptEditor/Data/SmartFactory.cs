using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Conditions.Data;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Parameters;

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
            IDatabaseProvider databaseProvider,
            IConditionDataManager conditionDataManager)
        {
            this.parameterFactory = parameterFactory;
            this.smartDataManager = smartDataManager;
            this.conditionDataManager = conditionDataManager;

            if (!parameterFactory.IsRegisteredLong("StoredTargetParameter"))
            {
                parameterFactory.Register("CreatureSpawnKeyParameter", new CreatureSpawnKeyParameter(databaseProvider));
                parameterFactory.Register("GameobjectSpawnKeyParameter", new GameObjectSpawnKeyParameter(databaseProvider));
                parameterFactory.Register("StoredTargetParameter", new VariableContextualParameter(GlobalVariableType.StoredTarget, "storedTarget"));
                parameterFactory.Register("DataVariableParameter", new VariableContextualParameter(GlobalVariableType.DataVariable, "data"));
                parameterFactory.Register("TimedEventParameter", new VariableContextualParameter(GlobalVariableType.TimedEvent, "timedEvent"));
                parameterFactory.Register("DoActionParameter", new VariableContextualParameter(GlobalVariableType.Action, "action"));
                parameterFactory.Register("DoFunctionParameter", new VariableContextualParameter(GlobalVariableType.Function, "function"));
                parameterFactory.Register("StoredPointParameter", new VariableContextualParameter(GlobalVariableType.StoredPoint, "storedPoint"));
                parameterFactory.Register("DatabasePointParameter", new VariableContextualParameter(GlobalVariableType.DatabasePoint, "databasePoint"));   
            }
        }

        public SmartEvent EventFactory(int id)
        {
            if (!smartDataManager.Contains(SmartType.SmartEvent, id))
                throw new InvalidSmartEventException(id);

            SmartEvent ev = new(id);
            ev.Chance.Value = 100;
            SmartGenericJsonData raw = smartDataManager.GetRawData(SmartType.SmartEvent, id);
            SetParameterObjects(ev, raw);
            return ev;
        }
        
        public void UpdateEvent(SmartEvent ev, int id)
        {
            if (ev.Id == id)
                return;

            SmartGenericJsonData raw = smartDataManager.GetRawData(SmartType.SmartEvent, id);
            SetParameterObjects(ev, raw, true);
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

        public void UpdateCondition(SmartCondition smartCondition, int id)
        {
            if (smartCondition.Id == id)
                return;

            SetParameterObjects(smartCondition, conditionDataManager.GetConditionData(id));
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

        public SmartAction ActionFactory(int id, SmartSource? source, SmartTarget? target)
        {
            if (!smartDataManager.Contains(SmartType.SmartAction, id))
                throw new InvalidSmartActionException(id);

            source ??= SourceFactory(0);
            target ??= TargetFactory(0);

            SmartAction action = new(id, source, target);
            var raw = smartDataManager.GetRawData(SmartType.SmartAction, id);
            action.ActionFlags = raw.Flags;
            action.CommentParameter.IsUsed = raw.CommentField != null;
            action.CommentParameter.Name = raw.CommentField ?? "Comment";
            SetParameterObjects(action, raw);
            UpdateTargetPositionVisibility(action.Target);

            return action;
        }

        private void UpdateTargetPositionVisibility(SmartTarget target)
        {
            var actionData = smartDataManager.GetRawData(SmartType.SmartAction, target.Parent?.Id ?? SmartConstants.ActionNone);
            var targetData = smartDataManager.GetRawData(SmartType.SmartTarget, target.Id);
            foreach (var t in target.Position)
                t.IsUsed = actionData.UsesTargetPosition | targetData.UsesTargetPosition;
        }

        public void UpdateAction(SmartAction smartAction, int id)
        {
            if (smartAction.Id == id)
                return;
            
            SmartGenericJsonData raw = smartDataManager.GetRawData(SmartType.SmartAction, id);
            smartAction.ActionFlags = raw.Flags;
            smartAction.CommentParameter.IsUsed = raw.CommentField != null;
            smartAction.CommentParameter.Name = raw.CommentField ?? "Comment";
            SetParameterObjects(smartAction, raw, true);
            UpdateTargetPositionVisibility(smartAction.Target);
        }

        public SmartAction ActionFactory(ISmartScriptLine line)
        {
            SmartSource source = SourceFactory(line);
            SmartTarget target = TargetFactory(line);

            var raw = smartDataManager.GetRawData(SmartType.SmartAction, line.ActionType);

            if (raw.ImplicitSource != null)
                UpdateSource(source, smartDataManager.GetDataByName(SmartType.SmartSource, raw.ImplicitSource).Id);
            
            SmartAction action = ActionFactory(line.ActionType, source, target);

            for (var i = 0; i < SmartAction.SmartActionParametersCount; ++i)
                action.GetParameter(i).Value = line.GetActionParam(i);
            
            if (raw.SourceStoreInAction)
            {
                try
                {
                    UpdateSource(source,
                        smartDataManager.GetRawData(SmartType.SmartSource, (int) action.GetParameter(2).Value).Id);
                    source.GetParameter(0).Value = action.GetParameter(3).Value;
                    source.GetParameter(1).Value = action.GetParameter(4).Value;
                    source.GetParameter(2).Value = action.GetParameter(5).Value;
                    action.GetParameter(2).Value = 0;
                    action.GetParameter(3).Value = 0;
                    action.GetParameter(4).Value = 0;
                    action.GetParameter(5).Value = 0;
                }
                catch (Exception)
                {
                }
            }
            
            return action;
        }

        public SmartTarget TargetFactory(int id)
        {
            if (!smartDataManager.Contains(SmartType.SmartTarget, id))
                throw new InvalidSmartTargetException(id);

            var data = smartDataManager.GetRawData(SmartType.SmartTarget, id);
            
            if (data.ReplaceWithId.HasValue)
                return TargetFactory(data.ReplaceWithId.Value);
            
            SmartTarget target = new(id);

            SetParameterObjects(target, data);

            var targetTypes = data.Types;

            if (targetTypes != null && targetTypes.Contains("Position"))
                target.IsPosition = true;

            return target;
        }

        public void UpdateTarget(SmartTarget smartTarget, int id)
        {
            if (smartTarget.Id == id)
                return;

            SmartGenericJsonData raw = smartDataManager.GetRawData(SmartType.SmartTarget, id);

            if (raw.ReplaceWithId.HasValue)
            {
                UpdateTarget(smartTarget, raw.ReplaceWithId.Value);
                return;
            }
            
            var targetTypes = raw.Types;
            smartTarget.IsPosition = targetTypes != null && targetTypes.Contains("Position");
            
            SetParameterObjects(smartTarget, raw, true);
            UpdateTargetPositionVisibility(smartTarget);
        }

        public SmartSource SourceFactory(int id)
        {
            if (!smartDataManager.Contains(SmartType.SmartSource, id))
                throw new InvalidSmartSourceException(id);

            var data = smartDataManager.GetRawData(SmartType.SmartSource, id);

            if (data.ReplaceWithId.HasValue)
                return SourceFactory(data.ReplaceWithId.Value);
            
            SmartSource source = new(id);

            SetParameterObjects(source, data);

            return source;
        }
        
        public void UpdateSource(SmartSource smartSource, int id)
        {
            if (smartSource.Id == id)
                return;
            
            SmartGenericJsonData raw = smartDataManager.GetRawData(SmartType.SmartSource, id);
            
            if (raw.ReplaceWithId.HasValue)
            {
                UpdateSource(smartSource, raw.ReplaceWithId.Value);
                return;
            }
            
            SetParameterObjects(smartSource, raw, true);
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

        private SmartSource SourceFactory(ISmartScriptLine line)
        {
            SmartSource source = SourceFactory(line.SourceType);

            source.Condition.Value = line.SourceConditionId;

            for (var i = 0; i < SmartSource.SmartSourceParametersCount; ++i)
                source.GetParameter(i).Value = line.GetSourceParam(i);

            return source;
        }

        private void SetParameterObjects(SmartBaseElement element, SmartGenericJsonData data, bool update = false)
        {
            if (data.DescriptionRules != null)
            {
                element.DescriptionRules = new List<DescriptionRule>();
                foreach (SmartDescriptionRulesJsonData rule in data.DescriptionRules)
                    element.DescriptionRules.Add(new DescriptionRule(rule));
            }
            else
                element.DescriptionRules = null;

            element.Id = data.Id;
            element.ReadableHint = data.Description;

            for (var i = 0; i < element.ParametersCount; ++i)
            {
                element.GetParameter(i).Name = "Parameter " + (i + 1) + " (unused)";
                element.GetParameter(i).IsUsed = false;
                element.GetParameter(i).ForceHidden = true;
            }

            if (data.Parameters == null)
            {
                for (var i = 0; i < element.ParametersCount; ++i)
                    element.GetParameter(i).ForceHidden = false;
                return;
            }

            for (var i = 0; i < data.Parameters.Count; ++i)
            {
                string key = data.Parameters[i].Type;
                if (data.Parameters[i].Values != null)
                {
                    key = $"{data.Name}_{i}";
                    if (!parameterFactory.IsRegisteredLong(key))
                        parameterFactory.Register(key, data.Parameters[i].Type == "FlagParameter" ? new FlagParameter(){Items = data.Parameters[i].Values} : new Parameter(){Items = data.Parameters[i].Values});
                }
                
                IParameter<long> parameter = parameterFactory.Factory(key);
                element.GetParameter(i).Name = data.Parameters[i].Name;
                if (!update)
                    element.GetParameter(i).Value = data.Parameters[i].DefaultVal;
                element.GetParameter(i).Parameter = parameter;
                element.GetParameter(i).IsUsed = true;
            }
            
            for (var i = 0; i < element.ParametersCount; ++i)
                element.GetParameter(i).ForceHidden = false;
        }
        
        private void SetParameterObjects(SmartBaseElement element, ConditionJsonData data)
        {
            element.Id = data.Id;
            element.ReadableHint = data.Description;

            for (var i = 0; i < element.ParametersCount; ++i)
                element.GetParameter(i).IsUsed = false;

            if (data.Parameters == null)
                return;

            for (var i = 0; i < data.Parameters.Count; ++i)
            {
                string key = data.Parameters[i].Type;
                if (data.Parameters[i].Values != null)
                {
                    key = $"{data.Name}_{i}";
                    if (!parameterFactory.IsRegisteredLong(key))
                        parameterFactory.Register(key, new Parameter(){Items = data.Parameters[i].Values});
                }
                
                IParameter<long> parameter = parameterFactory.Factory(key);

                element.GetParameter(i).Name = data.Parameters[i].Name;
                element.GetParameter(i).IsUsed = true;
                element.GetParameter(i).Parameter = parameter;
            }
        }
    }
}