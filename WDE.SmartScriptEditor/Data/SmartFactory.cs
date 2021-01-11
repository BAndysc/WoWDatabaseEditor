using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Parameters;
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

        public SmartFactory(IParameterFactory parameterFactory, ISmartDataManager smartDataManager)
        {
            this.parameterFactory = parameterFactory;
            this.smartDataManager = smartDataManager;
        }

        public SmartEvent EventFactory(int id)
        {
            if (!smartDataManager.Contains(SmartType.SmartEvent, id))
                throw new NullReferenceException("No data for event id " + id);

            var ev = new SmartEvent(id);
            var raw = smartDataManager.GetRawData(SmartType.SmartEvent, id);
            ev.Chance.SetValue(100);
            SetParameterObjects(ev, raw);

            if (raw.DescriptionRules != null)
            {
                ev.DescriptionRules = new List<DescriptionRule>();
                foreach (var rule in raw.DescriptionRules) ev.DescriptionRules.Add(new DescriptionRule(rule));
            }

            return ev;
        }

        public SmartEvent EventFactory(ISmartScriptLine line)
        {
            var ev = EventFactory(line.EventType);

            ev.Chance.SetValue(line.EventChance);
            ev.Phases.SetValue(line.EventPhaseMask);
            ev.Flags.SetValue(line.EventFlags);
            ev.CooldownMin.SetValue(line.EventCooldownMin);
            ev.CooldownMax.SetValue(line.EventCooldownMax);

            for (var i = 0; i < SmartEvent.SmartEventParamsCount; ++i)
                ev.SetParameter(i, GetEventParameter(line, i));

            return ev;
        }

        public SmartAction ActionFactory(int id, SmartSource source, SmartTarget target)
        {
            if (!smartDataManager.Contains(SmartType.SmartAction, id))
                throw new NullReferenceException("No data for action id " + id);

            var action = new SmartAction(id, source, target);

            SetParameterObjects(action, smartDataManager.GetRawData(SmartType.SmartAction, id));

            return action;
        }

        public SmartAction ActionFactory(ISmartScriptLine line)
        {
            var source = SourceFactory(line);
            var target = TargetFactory(line);

            var action = ActionFactory(line.ActionType, source, target);

            for (var i = 0; i < SmartAction.SmartActionParametersCount; ++i)
                action.SetParameter(i, GetActionParameter(line, i));

            return action;
        }

        public SmartTarget TargetFactory(int id)
        {
            if (!smartDataManager.Contains(SmartType.SmartTarget, id))
                throw new NullReferenceException("No data for target id " + id);

            var target = new SmartTarget(id);

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

            var source = new SmartSource(id);

            SetParameterObjects(source, smartDataManager.GetRawData(SmartType.SmartSource, id));

            return source;
        }

        private int GetEventParameter(ISmartScriptLine line, int i)
        {
            // ugly but DB is in such form
            switch (i)
            {
                case 0:
                    return line.EventParam1;
                case 1:
                    return line.EventParam2;
                case 2:
                    return line.EventParam3;
                case 3:
                    return line.EventParam4;
            }

            throw new ArgumentException("Event parameter out of range");
        }

        private int GetActionParameter(ISmartScriptLine line, int i)
        {
            // ugly but DB is in such form
            switch (i)
            {
                case 0:
                    return line.ActionParam1;
                case 1:
                    return line.ActionParam2;
                case 2:
                    return line.ActionParam3;
                case 3:
                    return line.ActionParam4;
                case 4:
                    return line.ActionParam5;
                case 5:
                    return line.ActionParam6;
            }

            throw new ArgumentException("Action parameter out of range");
        }

        public SmartTarget TargetFactory(ISmartScriptLine line)
        {
            var target = TargetFactory(line.TargetType);

            target.X = line.TargetX;
            target.Y = line.TargetY;
            target.Z = line.TargetZ;
            target.O = line.TargetO;

            target.Condition.SetValue(line.TargetConditionId);

            for (var i = 0; i < SmartSource.SmartSourceParametersCount; ++i)
                target.SetParameter(i, GetTargetParameter(line, i));

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
            var source = SourceFactory(line.SourceType);

            source.Condition.SetValue(line.SourceConditionId);

            for (var i = 0; i < SmartSource.SmartSourceParametersCount; ++i)
                source.SetParameter(i, GetSourceParameter(line, i));

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
                var parameter = parameterFactory.Factory(data.Parameters[i].Type,
                    data.Parameters[i].Name,
                    data.Parameters[i].DefaultVal);
                parameter.Description = data.Parameters[i].Description;
                if (data.Parameters[i].Values != null)
                    parameter.Items = data.Parameters[i].Values;
                element.SetParameterObject(i, parameter);
            }
        }
    }
}