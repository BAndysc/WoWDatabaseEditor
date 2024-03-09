using System;
using System.Collections.Generic;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.EventAiEditor.Models;
using WDE.EventAiEditor.Parameters;

namespace WDE.EventAiEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class EventAiFactory : IEventAiFactory
    {
        private readonly IParameterFactory parameterFactory;
        private readonly IEventAiDataManager eventAiDataManager;

        public EventAiFactory(IParameterFactory parameterFactory, 
            ITableEditorPickerService tableEditorPickerService,
            IEventAiDataManager eventAiDataManager,
            IMangosDatabaseProvider mangosDatabaseProvider,
            IParameterPickerService parameterPickerService,
            IDatabaseProvider databaseProvider)
        {
            this.parameterFactory = parameterFactory;
            this.eventAiDataManager = eventAiDataManager;
            if (!parameterFactory.IsRegisteredLong("EventAiPhaseParameter"))
            {
                parameterFactory.Register("ActionSummonIdParameter", new ActionSummonIdParameter(mangosDatabaseProvider, tableEditorPickerService));
                parameterFactory.Register("StartRelayIdParameter", new StartRelayIdParameter(mangosDatabaseProvider, tableEditorPickerService));
                parameterFactory.Register("EventAiNewTextRandomTemplateParameter", new EventAiNewTextRandomTemplateParameter(mangosDatabaseProvider,  tableEditorPickerService));
                parameterFactory.Register("EventAiNewTextParameter", new EventAiNewTextParameter(mangosDatabaseProvider,  tableEditorPickerService));
                parameterFactory.Register("EventAiPhaseMaskParameter", EventAiPhaseMaskParameter.Instance);
                parameterFactory.Register("EventAiPhaseParameter", EventAiPhaseParameter.Instance);
                parameterFactory.RegisterCombined("MangosEventAiEmoteParameter", "EmoteOneShotParameter", "EmoteStateParameter", (a, b) => new MangosEmoteParameter(a, b));
                parameterFactory.RegisterCombined("EventAiSetFieldValueParameter", "ItemParameter", "DynamicFlagsParameter", "NpcFlagParameter", (a, b, c) => new EventAiSetFieldValueParameter(parameterPickerService, a, b, c));
            }
        }

        public EventAiEvent EventFactory(uint id)
        {
            if (!eventAiDataManager.Contains(EventOrAction.Event, id))
                throw new InvalidEventException(id);

            EventAiEvent ev = new(id);
            ev.Chance.Value = 100;
            EventActionGenericJsonData raw = eventAiDataManager.GetRawData(EventOrAction.Event, id);
            SetParameterObjects(ev, raw);
            return ev;
        }
        
        public void UpdateEvent(EventAiEvent ev, uint id)
        {
            if (ev.Id == id)
                return;

            EventActionGenericJsonData raw = eventAiDataManager.GetRawData(EventOrAction.Event, id);
            SetParameterObjects(ev, raw, true);
        }
        
        public EventAiEvent EventFactory(IEventAiLine line)
        {
            EventAiEvent ev = EventFactory(line.EventType);

            ev.Chance.Value = line.EventChance;
            ev.Phases.Value = line.EventInversePhaseMask;
            ev.Flags.Value = line.EventFlags;

            for (var i = 0; i < EventAiEvent.EventParamsCount; ++i)
            {
                ev.GetParameter(i).Value = line.GetEventParam(i);
            }

            return ev;
        }

        public EventAiAction ActionFactory(uint id)
        {
            if (!eventAiDataManager.Contains(EventOrAction.Action, id))
                throw new InvalidActionException(id);
            
            EventAiAction action = new(id);
            var raw = eventAiDataManager.GetRawData(EventOrAction.Action, id);
            action.ActionFlags = raw.Flags;
            action.CommentParameter.IsUsed = raw.CommentField != null;
            action.CommentParameter.Name = raw.CommentField ?? "Comment";
            SetParameterObjects(action, raw);

            return action;
        }

        public void UpdateAction(EventAiAction eventAiAction, uint id)
        {
            if (eventAiAction.Id == id)
                return;
            
            EventActionGenericJsonData raw = eventAiDataManager.GetRawData(EventOrAction.Action, id);
            eventAiAction.ActionFlags = raw.Flags;
            eventAiAction.CommentParameter.IsUsed = raw.CommentField != null;
            eventAiAction.CommentParameter.Name = raw.CommentField ?? "Comment";
            SetParameterObjects(eventAiAction, raw, true);
        }

        public EventAiAction? ActionFactory(IEventAiLine line, int actionIndex)
        {
            if (actionIndex > 0 && (int)line.GetActionType(actionIndex) == EventAiConstants.ActionDoNothing)
                return null;
            
            var raw = eventAiDataManager.GetRawData(EventOrAction.Action, line.GetActionType(actionIndex));

            EventAiAction action = ActionFactory(raw.Id);

            for (var i = 0; i < EventAiAction.ActionParametersCount; ++i)
                action.GetParameter(i).Value = line.GetActionParameter(actionIndex, i);
            
            return action;
        }

        private void SetParameterObjects(EventAiBaseElement element, EventActionGenericJsonData data, bool update = false)
        {
            if (data.DescriptionRules != null)
            {
                element.DescriptionRules = new List<DescriptionRule>();
                foreach (EventDescriptionRulesJsonData rule in data.DescriptionRules)
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
                if (!parameterFactory.IsRegisteredLong(key))
                    LOG.LogWarning("Parameter type " + key + " is not registered");
                
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
    }
}