using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Models;
using Prism.Ioc;

namespace WDE.SmartScriptEditor.Data
{
    [Common.Attributes.AutoRegister]
    public class SmartFactory : ISmartFactory
    {
        private readonly IParameterFactory _parameterFactory;

        public SmartFactory(IParameterFactory parameterFactory)
        {
            _parameterFactory = parameterFactory;
        }

        public SmartEvent EventFactory(int id)
        {
            if (!SmartDataManager.GetInstance().Contains(SmartType.SmartEvent, id))
                throw new NullReferenceException("No data for event id " + id);

            SmartEvent ev = new SmartEvent(id);
            var raw = SmartDataManager.GetInstance().GetRawData(SmartType.SmartEvent, id);
            ev.Chance.SetValue(100);
            SetParameterObjects(ev, raw);

            if (raw.DescriptionRules != null)
            {
                ev.DescriptionRules = new List<DescriptionRule>();
                foreach (var rule in raw.DescriptionRules)
                {
                    ev.DescriptionRules.Add(new DescriptionRule(rule));
                }
            }

            return ev;
        }

        public SmartEvent EventFactory(ISmartScriptLine line)
        {
            SmartEvent ev = EventFactory(line.EventType);

            ev.Chance.SetValue(line.EventChance);
            ev.Phases.SetValue(line.EventPhaseMask);
            ev.Flags.SetValue(line.EventFlags);
            ev.CooldownMin.SetValue(line.EventCooldownMin);
            ev.CooldownMax.SetValue(line.EventCooldownMax);

            for (int i = 0; i < SmartEvent.SmartEventParamsCount; ++i)
                ev.SetParameter(i, GetEventParameter(line, i));

            return ev;
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

        public SmartAction ActionFactory(int id, SmartSource source, SmartTarget target)
        {
            if (!SmartDataManager.GetInstance().Contains(SmartType.SmartAction, id))
                throw new NullReferenceException("No data for action id " + id);

            SmartAction action = new SmartAction(id, source, target);

            SetParameterObjects(action, SmartDataManager.GetInstance().GetRawData(SmartType.SmartAction, id));

            return action;
        }

        public SmartAction ActionFactory(ISmartScriptLine line)
        {
            SmartSource source = SourceFactory(line);
            SmartTarget target = TargetFactory(line);

            SmartAction action = ActionFactory(line.ActionType, source, target);

            for (int i = 0; i < SmartAction.SmartActionParametersCount; ++i)
                action.SetParameter(i, GetActionParameter(line, i));

            return action;
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

        public SmartTarget TargetFactory(int id)
        {
            if (!SmartDataManager.GetInstance().Contains(SmartType.SmartTarget, id))
                throw new NullReferenceException("No data for target id " + id);

            SmartTarget target = new SmartTarget(id);

            SetParameterObjects(target, SmartDataManager.GetInstance().GetRawData(SmartType.SmartTarget, id));

            if (SmartDataManager.GetInstance().GetRawData(SmartType.SmartTarget, id).Types.Contains("Position"))
                target.IsPosition = true;

            return target;
        }

        public SmartTarget TargetFactory(ISmartScriptLine line)
        {
            SmartTarget target = TargetFactory(line.TargetType);

            target.X = line.TargetX;
            target.Y = line.TargetY;
            target.Z = line.TargetZ;
            target.O = line.TargetO;

            target.Condition.SetValue(line.TargetConditionId);

            for (int i = 0; i < SmartTarget.SmartSourceParametersCount; ++i)
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

        public SmartSource SourceFactory(int id)
        {
            if (!SmartDataManager.GetInstance().Contains(SmartType.SmartSource, id))
                throw new NullReferenceException("No data for source id " + id);

            SmartSource source = new SmartSource(id);

            SetParameterObjects(source, SmartDataManager.GetInstance().GetRawData(SmartType.SmartSource, id));

            return source;
        }

        private SmartSource SourceFactory(ISmartScriptLine line)
        {
            SmartSource source = SourceFactory(line.SourceType);

            source.Condition.SetValue(line.SourceConditionId);

            for (int i = 0; i < SmartSource.SmartSourceParametersCount; ++i)
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
            
            for (int i = 0; i < data.Parameters.Count; ++i)
            {
                var parameter = _parameterFactory.Factory(data.Parameters[i].Type, data.Parameters[i].Name, data.Parameters[i].DefaultVal);
                parameter.Description = data.Parameters[i].Description;
                if (data.Parameters[i].Values != null)
                    parameter.Items = data.Parameters[i].Values;
                element.SetParameterObject(i, parameter);
            }
        }
    }
}
