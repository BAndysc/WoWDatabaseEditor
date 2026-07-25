using System;
using System.Collections.Generic;
using Prism.Ioc;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Conditions.Data;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Parameters;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    [SingleInstance]
    public class SmartFactory : ISmartFactory
    {
        private readonly IParameterFactory parameterFactory;
        private readonly IEditorFeatures editorFeatures;
        private readonly ISmartDataManager smartDataManager;
        private readonly IConditionDataManager conditionDataManager;
        private readonly ICurrentCoreVersion currentCoreVersion;

        public SmartFactory(IParameterFactory parameterFactory, 
            IEditorFeatures editorFeatures,
            ISmartDataManager smartDataManager, 
            ICachedDatabaseProvider databaseProvider,
            IConditionDataManager conditionDataManager,
            ITableEditorPickerService tableEditorPickerService,
            IItemFromListProvider itemFromListProvider,
            ICurrentCoreVersion currentCoreVersion,
            IQuestEntryProviderService questEntryProviderService,
            IContainerProvider containerProvider,
            IParameterPickerService pickerService)
        {
            this.parameterFactory = parameterFactory;
            this.editorFeatures = editorFeatures;
            this.smartDataManager = smartDataManager;
            this.conditionDataManager = conditionDataManager;
            this.currentCoreVersion = currentCoreVersion;

            if (!parameterFactory.IsRegisteredLong("StoredTargetParameter"))
            {
                parameterFactory.Register("PathParameter", new WaypointsParameter(tableEditorPickerService));
                parameterFactory.Register("PathPointParameter", new WaypointsPointParameter(tableEditorPickerService, pickerService));
                parameterFactory.Register("LinkParameter", new Parameter());
                parameterFactory.Register("TimedActionListParameter", containerProvider.Resolve<TimedActionListParameter>());
                parameterFactory.Register("GossipMenuOptionParameter", new GossipMenuOptionParameter(databaseProvider, tableEditorPickerService, itemFromListProvider));
                parameterFactory.Register("CreatureTextParameter", new CreatureTextParameter(smartDataManager, databaseProvider, tableEditorPickerService, itemFromListProvider, DatabaseTable.WorldTable("creature_text"), "GroupId"));
                parameterFactory.Register("QuestStarterParameter", new QuestStarterEnderParameter(databaseProvider, tableEditorPickerService, questEntryProviderService, "queststarter"));
                parameterFactory.Register("QuestEnderParameter", new QuestStarterEnderParameter(databaseProvider, tableEditorPickerService, questEntryProviderService, "questender"));
                parameterFactory.Register("CreatureSpawnKeyParameter", new CreatureSpawnKeyParameter(databaseProvider, itemFromListProvider));
                parameterFactory.Register("GameobjectSpawnKeyParameter", new GameObjectSpawnKeyParameter(databaseProvider, itemFromListProvider));
                parameterFactory.Register("SmartScenarioStepParameter", containerProvider.Resolve<SmartScenarioStepParameter>());
                parameterFactory.Register("SmartQuestObjectiveStorageIndexParameter", containerProvider.Resolve<SmartQuestObjectiveParameter>((typeof(bool), true)));
                parameterFactory.Register("SmartQuestObjectiveParameter", containerProvider.Resolve<SmartQuestObjectiveParameter>((typeof(bool), false)));
                parameterFactory.RegisterCombined("NpcFlagsSmartTypeBasedParameter", "NpcFlagParameter", "NpcFlag2Parameter", (npc1, npc2) => new NpcFlagsSmartTypeBasedParameter(npc1, npc2, pickerService));
                var storedTarget = parameterFactory.Register("StoredTargetParameter", containerProvider.Resolve<VariableContextualParameter>(
                    (typeof(GlobalVariableType), GlobalVariableType.StoredTarget), (typeof(string), "storedTarget")));
                parameterFactory.Register("DataVariableParameter", containerProvider.Resolve<VariableContextualParameter>(
                    (typeof(GlobalVariableType), GlobalVariableType.DataVariable), (typeof(string), "data")));
                parameterFactory.Register("TimedEventParameter", containerProvider.Resolve<VariableContextualParameter>(
                    (typeof(GlobalVariableType), GlobalVariableType.TimedEvent), (typeof(string), "timedEvent")));
                parameterFactory.Register("DoActionParameter", containerProvider.Resolve<VariableContextualParameter>(
                    (typeof(GlobalVariableType), GlobalVariableType.Action), (typeof(string), "action")));
                parameterFactory.Register("DoFunctionParameter", containerProvider.Resolve<VariableContextualParameter>(
                    (typeof(GlobalVariableType), GlobalVariableType.Function), (typeof(string), "function")));
                parameterFactory.Register("StoredPointParameter", containerProvider.Resolve<VariableContextualParameter>(
                    (typeof(GlobalVariableType), GlobalVariableType.StoredPoint), (typeof(string), "storedPoint")));
                parameterFactory.Register("DatabasePointParameter", containerProvider.Resolve<VariableContextualParameter>(
                    (typeof(GlobalVariableType), GlobalVariableType.DatabasePoint), (typeof(string), "databasePoint")));
                parameterFactory.Register("RepatedParameter", containerProvider.Resolve<VariableContextualParameter>(
                    (typeof(GlobalVariableType), GlobalVariableType.Repeated), (typeof(string), "repeated")));
                parameterFactory.Register("MapEventParameter", containerProvider.Resolve<VariableContextualParameter>(
                    (typeof(GlobalVariableType), GlobalVariableType.MapEvent), (typeof(string), "map event")));
                var actor = parameterFactory.Register("ActorParameter", containerProvider.Resolve<VariableContextualParameter>(
                    (typeof(GlobalVariableType), GlobalVariableType.Actor), (typeof(string), "actor")));
                parameterFactory.Register("StoredTargetOrActorParameter", new StoredTargetOrActorParameter(storedTarget, actor));
                // SMART_ACTION_FOLLOW/_AC "Credit": creature entry when Credit Type (param index 4) = 0 (Monster kill),
                // quest id when Credit Type = 1 (Event) — see RewardPlayerAndGroupAtEvent(uint32 creature_id, ...) vs
                // GroupEventHappens(uint32 questId, ...) in SmartAI::StopFollow.
                parameterFactory.RegisterCombined("FollowCreditParameter", new[] { "CreatureParameter", "QuestParameter" },
                    prams => new SwitchedByParameter(4, new Dictionary<long, IParameter<long>> { [0] = prams[0], [1] = prams[1] }, prams[0]));
                // SMART_ACTION_SET_UNIT_FLAG/REMOVE_UNIT_FLAG(+_HIDDEN) "Flags": bitmask is against UNIT_FIELD_FLAGS
                // when sibling "Type" (param index 1) = 0, or UNIT_FIELD_FLAGS_2 when Type = 1.
                parameterFactory.RegisterCombined("UnitFlagsSwitchedParameter", new[] { "UnitFlagParameter", "UnitFlags2Parameter" },
                    prams => new SwitchedByParameter(1, new Dictionary<long, IParameter<long>> { [0] = prams[0], [1] = prams[1] }, prams[0]));
                // SMART_EVENT_NEAR_UNIT_AC/NEAR_UNIT_NEGATION_AC "Entry": a creature entry when sibling
                // "Type" (param index 0) = 0, or a gameobject entry when Type = 1.
                parameterFactory.RegisterCombined("NearUnitEntryParameter", new[] { "CreatureParameter", "GameobjectParameter" },
                    prams => new SwitchedByParameter(0, new Dictionary<long, IParameter<long>> { [0] = prams[0], [1] = prams[1] }, prams[0]));
                // SMART_ACTION_SET_UNIT_FIELD_BYTES_1 "Value": meaning depends on sibling "Type" (param index 1) —
                // 0=stand state, 1=pet talents (no-op), 2=vis flag (raw), 3=anim tier.
                parameterFactory.RegisterCombined("UnitFieldBytes1ValueParameter", new[] { "StandStateParameter", "Parameter", "AnimTierParameter" },
                    prams => new SwitchedByParameter(1, new Dictionary<long, IParameter<long>> { [0] = prams[0], [1] = prams[1], [2] = prams[1], [3] = prams[2] }, prams[0]));
                // SMART_ACTION_REMOVE_UNIT_FIELD_BYTES_1 "Bytes": only meaningful when sibling "Type" (param index 1) = 2 (vis flag);
                // Type 0/1/3 ignore this value entirely on TrinityCore.
                parameterFactory.RegisterCombined("UnitFieldBytes1RemoveValueParameter", new[] { "UnusedParameter", "Parameter" },
                    prams => new SwitchedByParameter(1, new Dictionary<long, IParameter<long>> { [0] = prams[0], [1] = prams[0], [2] = prams[1], [3] = prams[0] }, prams[0]));
                // SMART_ACTION_ADD_IMMUNITY_AC/REMOVE_IMMUNITY_AC "Value": Unit::ApplySpellImmune's `type` (SpellImmunity
                // category, param index 0) selects which enum the "Value" field is read against.
                parameterFactory.RegisterCombined("SpellImmunityValueParameter",
                    new[] { "SpellEffectTypeParameter", "AuraTypeParameter", "SpellSchoolMaskParameter", "DispelTypeParameter", "MechanicsParameter", "SpellParameter", "Parameter" },
                    prams => new SwitchedByParameter(0, new Dictionary<long, IParameter<long>>
                    {
                        [0] = prams[0], [1] = prams[1], [2] = prams[2], [3] = prams[2],
                        [4] = prams[3], [5] = prams[4], [6] = prams[5], [7] = prams[5]
                    }, prams[6]));
            }
        }

        public SmartEvent EventFactory(SmartScriptBase? parent, int id)
        {
            if (!smartDataManager.Contains(SmartType.SmartEvent, id))
                throw new InvalidSmartEventException(id);

            SmartEvent ev = new(id, editorFeatures);
            ev.Parent = parent;
            ev.Chance.Value = 100;
            SmartGenericJsonData raw = smartDataManager.GetRawData(SmartType.SmartEvent, id);
            SetParameterObjects(ev.Parent, ev, raw);
            return ev;
        }
        
        public void UpdateEvent(SmartEvent ev, int id)
        {
            if (ev.Id == id)
                return;

            SmartGenericJsonData raw = smartDataManager.GetRawData(SmartType.SmartEvent, id);
            SetParameterObjects(ev.Parent, ev, raw, true);
        }
        
        public SmartEvent EventFactory(ISmartScriptLine line)
        {
            // pass null as parent, it will be set later
            SmartEvent ev = EventFactory(null, line.EventType);

            ev.Chance.Value = line.EventChance;
            ev.Phases.Value = line.EventPhaseMask;
            ev.Flags.Value = line.EventFlags;
            ev.CooldownMin.Value = line.EventCooldownMin;
            ev.CooldownMax.Value = line.EventCooldownMax;
            ev.TimerId.Value = line.TimerId;

            for (var i = 0; i < ev.ParametersCount; ++i)
                ev.GetParameter(i).Value = line.GetEventParam(i);
            
            for (var i = 0; i < ev.FloatParametersCount; ++i)
                ev.GetFloatParameter(i).Value = line.GetEventFloatParam(i);
            
            for (var i = 0; i < ev.StringParametersCount; ++i)
                ev.GetStringParameter(i).Value = line.GetEventStringParam(i) ?? "";

            return ev;
        }

        public SmartCondition ConditionFactory(int id)
        {
            if (!conditionDataManager.HasConditionData(id))
                throw new NullReferenceException("No data for condition id " + id);

            SmartCondition ev = new(id, editorFeatures);
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

        public SmartCondition ConditionFactory(ICondition line)
        {
            SmartCondition condition = ConditionFactory(line.ConditionIndex == 0 ? line.ConditionType : -line.ConditionType);

            condition.Inverted.Value = line.NegativeCondition;
            condition.ConditionTarget.Value = line.ConditionTarget;
            for (int i = 0; i < condition.ParametersCount; ++i)
                condition.GetParameter(i).Value = line.GetConditionValue(i);
            for (int i = 0; i < condition.StringParametersCount; ++i)
                condition.GetStringParameter(i).Value = line.GetConditionValueString(i);

            return condition;
        }

        public SmartAction ActionFactory(int id, SmartSource? source, SmartTarget? target)
        {
            if (!smartDataManager.Contains(SmartType.SmartAction, id))
                throw new InvalidSmartActionException(id);

            source ??= SourceFactory(0);
            target ??= TargetFactory(0);

            SmartAction action = new(id, editorFeatures, source, target);
            var raw = smartDataManager.GetRawData(SmartType.SmartAction, id);
            action.ActionFlags = raw.Flags;
            action.CommentParameter.IsUsed = raw.CommentField != null;
            action.CommentParameter.Name = raw.CommentField ?? "Comment";
            SetParameterObjects(action.Parent?.Parent, action, raw);
            UpdateTargetPositionVisibility(action.Target);

            return action;
        }

        private static readonly string[] Coords = new[] { "X", "Y", "Z", "O" }; 

        private void UpdateTargetPositionVisibility(SmartSource sourceOrTarget)
        {
            var actionData = smartDataManager.GetRawData(SmartType.SmartAction, sourceOrTarget.Parent?.Id ?? SmartConstants.ActionNone);
            var targetData = smartDataManager.GetRawData(SmartType.SmartTarget, sourceOrTarget.Id);
            for (int i = 0; i < sourceOrTarget.FloatParametersCount; ++i)
            {
                var parameter = sourceOrTarget.GetFloatParameter(i);
                var isCustomParameter = targetData.FloatParameters != null && targetData.FloatParameters.Count > i;
                parameter.IsUsed = actionData.UsesTargetPosition || 
                                   targetData.UsesTargetPosition || 
                                   isCustomParameter;
                if (parameter.IsUsed && !isCustomParameter)
                    parameter.Name = Coords[i];
            }
        }

        public void UpdateAction(SmartAction smartAction, int id)
        {
            if (smartAction.Id == id)
                return;
            
            SmartGenericJsonData raw = smartDataManager.GetRawData(SmartType.SmartAction, id);
            smartAction.ActionFlags = raw.Flags;
            smartAction.CommentParameter.IsUsed = raw.CommentField != null;
            smartAction.CommentParameter.Name = raw.CommentField ?? "Comment";
            SetParameterObjects(smartAction.Parent?.Parent, smartAction, raw, true);
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

            for (var i = 0; i < action.ParametersCount; ++i)
                action.GetParameter(i).Value = line.GetActionParam(i);
            
            for (var i = 0; i < action.FloatParametersCount; ++i)
                action.GetFloatParameter(i).Value = line.GetActionFloatParam(i);
            
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
            
            SmartTarget target = new(id, editorFeatures);

            SetParameterObjects(target.Parent?.Parent?.Parent, target, data);

            var targetTypes = data.RawTypes;

            if (targetTypes.HasFlagFast(SmartSourceTargetType.Position))
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
            
            var targetTypes = raw.RawTypes;
            smartTarget.IsPosition = targetTypes.HasFlagFast(SmartSourceTargetType.Position);
            
            SetParameterObjects(smartTarget.Parent?.Parent?.Parent, smartTarget, raw, true);
            UpdateTargetPositionVisibility(smartTarget);
        }

        public SmartSource SourceFactory(int id)
        {
            if (!smartDataManager.Contains(SmartType.SmartSource, id))
            {
                // target only target used as a source. Allow it anyway
                if (smartDataManager.Contains(SmartType.SmartTarget, id))
                    return TargetFactory(id);
                else
                    throw new InvalidSmartSourceException(id);
            }

            var data = smartDataManager.GetRawData(SmartType.SmartSource, id);

            if (data.ReplaceWithId.HasValue)
                return SourceFactory(data.ReplaceWithId.Value);
            
            SmartSource source = new(id, editorFeatures);

            SetParameterObjects(source.Parent?.Parent?.Parent, source, data);

            var sourceTypes = data.RawTypes;

            if (sourceTypes.HasFlagFast(SmartSourceTargetType.Position))
                source.IsPosition = true;
            
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
            
            SetParameterObjects(smartSource.Parent?.Parent?.Parent, smartSource, raw, true);
            
            var sourceTypes = raw.RawTypes;
            smartSource.IsPosition = sourceTypes.HasFlagFast(SmartSourceTargetType.Position);
            UpdateTargetPositionVisibility(smartSource);
        }
        
        public SmartTarget TargetFactory(ISmartScriptLine line)
        {
            SmartTarget target = TargetFactory(line.TargetType);

            target.X = line.TargetX;
            target.Y = line.TargetY;
            target.Z = line.TargetZ;
            target.O = line.TargetO;

            target.Condition.Value = (line.TargetConditionId);

            for (var i = 0; i < target.ParametersCount; ++i)
                target.GetParameter(i).Value = line.GetTargetParam(i);

            return target;
        }

        private SmartSource SourceFactory(ISmartScriptLine line)
        {
            SmartSource source = SourceFactory(line.SourceType);

            if (editorFeatures.SourceHasPosition)
            {
                source.X = line.SourceX;
                source.Y = line.SourceY;
                source.Z = line.SourceZ;
                source.O = line.SourceO;
            }
            
            source.Condition.Value = line.SourceConditionId;

            for (var i = 0; i < source.ParametersCount; ++i)
                source.GetParameter(i).Value = line.GetSourceParam(i);

            return source;
        }

        private void SetParameterObjects(SmartScriptBase? script, SmartBaseElement element, SmartGenericJsonData data, bool update = false)
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
            
            for (var i = 0; i < element.FloatParametersCount; ++i)
            {
                element.GetFloatParameter(i).Name = "Float parameter " + (i + 1) + " (unused)";
                element.GetFloatParameter(i).IsUsed = false;
                element.GetFloatParameter(i).ForceHidden = true;
            }
            
            for (var i = 0; i < element.StringParametersCount; ++i)
            {
                element.GetStringParameter(i).Name = "String parameter " + (i + 1) + " (unused)";
                element.GetStringParameter(i).IsUsed = false;
                element.GetStringParameter(i).ForceHidden = true;
            }

            if (data.Parameters == null)
            {
                for (var i = 0; i < element.ParametersCount; ++i)
                    element.GetParameter(i).ForceHidden = false;
            }
            else
            {
                for (var i = 0; i < data.Parameters.Count && i < element.ParametersCount; ++i)
                {
                    string key = data.Parameters[i].Type;
                    if (!parameterFactory.IsRegisteredLong(key))
                        LOG.LogWarning("Parameter type " + key + " is not registered");
                
                    IParameter<long> parameter = parameterFactory.Factory(key);
                    element.GetParameter(i).Name = data.Parameters[i].Name;
                    if (!update)
                        element.GetParameter(i).Value = data.Parameters[i].GetEffectiveDefaultValue(script);
                    element.GetParameter(i).Parameter = parameter;
                    element.GetParameter(i).IsUsed = parameter != UnusedParameter.Instance;
                }
            
                for (var i = 0; i < element.ParametersCount; ++i)
                    element.GetParameter(i).ForceHidden = false;   
            }
            
            if (data.FloatParameters == null)
            {
                for (var i = 0; i < element.FloatParametersCount; ++i)
                    element.GetFloatParameter(i).ForceHidden = false;
            }
            else
            {
                for (var i = 0; i < data.FloatParameters.Count; ++i)
                {
                    element.GetFloatParameter(i).Name = data.FloatParameters[i].Name;
                    if (!update)
                        element.GetFloatParameter(i).Value = data.FloatParameters[i].DefaultVal;
                    element.GetFloatParameter(i).IsUsed = true;
                }
            
                for (var i = 0; i < element.FloatParametersCount; ++i)
                    element.GetFloatParameter(i).ForceHidden = false;   
            }
            
            if (data.StringParameters == null)
            {
                for (var i = 0; i < element.StringParametersCount; ++i)
                    element.GetStringParameter(i).ForceHidden = false;
            }
            else
            {
                for (var i = 0; i < data.StringParameters.Count; ++i)
                {
                    element.GetStringParameter(i).Name = data.StringParameters[i].Name;
                    if (!update)
                        element.GetStringParameter(i).Value = data.StringParameters[i].DefaultVal;
                    element.GetStringParameter(i).IsUsed = true;
                }
            
                for (var i = 0; i < element.StringParametersCount; ++i)
                    element.GetStringParameter(i).ForceHidden = false;   
            }
        }
        
        private void SetParameterObjects(SmartCondition element, ConditionJsonData data)
        {
            element.Id = data.Id;
            element.ReadableHint = data.Description;
            element.NegativeReadableHint = data.NegativeDescription;

            for (var i = 0; i < element.ParametersCount; ++i)
                element.GetParameter(i).IsUsed = false;

            for (var i = 0; i < element.StringParametersCount; ++i)
                element.GetStringParameter(i).IsUsed = false;
            
            if (data.Parameters != null)
            {
                for (var i = 0; i < data.Parameters.Count && i < element.ParametersCount; ++i)
                {
                    string key = data.Parameters[i].Type;
                    if (!parameterFactory.IsRegisteredLong(key))
                        LOG.LogWarning("Parameter type " + key + " is not registered");
                    IParameter<long> parameter = parameterFactory.Factory(key);

                    element.GetParameter(i).Name = data.Parameters[i].Name;
                    element.GetParameter(i).IsUsed = true;
                    element.GetParameter(i).Parameter = parameter;
                }   
            }

            if (data.StringParameters != null)
            {
                for (var i = 0; i < data.StringParameters.Count; ++i)
                {
                    string? key = data.StringParameters[i].Type;
                    if (key != null && !parameterFactory.IsRegisteredString(key))
                        LOG.LogWarning("Parameter type " + key + " is not registered");
                    IParameter<string> parameter = parameterFactory.FactoryString(key);

                    element.GetStringParameter(i).Name = data.StringParameters[i].Name;
                    element.GetStringParameter(i).IsUsed = true;
                    element.GetStringParameter(i).Parameter = parameter;
                }
            }
        }
    }
}