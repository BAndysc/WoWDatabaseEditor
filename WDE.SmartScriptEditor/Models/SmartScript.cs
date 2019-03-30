﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WDE.Common.Database;
using WDE.SmartScriptEditor.Data;
using Prism.Ioc;
using WDE.Conditions.Model;
using WDE.Conditions.Data;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartScript
    {
        public readonly ObservableCollection<SmartEvent> Events;
        public readonly int EntryOrGuid;
        public readonly SmartScriptType SourceType;
        private readonly ISmartFactory smartFactory;
        private readonly IConditionDataManager conditionDataManager;

        public SmartScript(SmartScriptSolutionItem item, ISmartFactory smartFactory, IConditionDataManager conditionDataManager)
        {
            EntryOrGuid = item.Entry;
            SourceType = item.SmartType;
            Events = new ObservableCollection<SmartEvent>();
            this.smartFactory = smartFactory;
            this.conditionDataManager = conditionDataManager;
        }

        public void Load(IEnumerable<ISmartScriptLine> lines, IEnumerable<IConditionLine> conditions)
        {
            int? entry = null;
            SmartScriptType? source = null;
            int previousLink = -1;
            SmartEvent currentEvent = null;

            foreach (var line in lines)
            {
                if (!entry.HasValue)
                    entry = line.EntryOrGuid;
                else
                    Debug.Assert(entry.Value == line.EntryOrGuid);

                if (!source.HasValue)
                    source = (SmartScriptType)line.ScriptSourceType;
                else
                    Debug.Assert((int)source.Value == line.ScriptSourceType);

                if (previousLink != line.Id)
                {
                    currentEvent = SafeEventFactory(line);
                    if (currentEvent != null)
                    {
                        SetConditionsForEvent(currentEvent, conditions);
                        currentEvent.conditionDataManager = conditionDataManager;
                        Events.Add(currentEvent);
                    }   
                    else
                        continue;
                }

                var action = SafeActionFactory(line);
                if (action != null)
                    currentEvent.AddAction(action);

                previousLink = line.Link;
            }
        }

        private SmartAction SafeActionFactory(ISmartScriptLine line)
        {
            try
            {
               return smartFactory.ActionFactory(line);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show($"Action {line.ActionType} unknown, skipping action");
            }
            return null;
        }

        private SmartEvent SafeEventFactory(ISmartScriptLine line)
        {
            try
            {
                return smartFactory.EventFactory(line);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show($"Event {line.EventType} unknown, skipping action");
            }
            return null;
        }

        private void SetConditionsForEvent(SmartEvent smartEvent, IEnumerable<IConditionLine> conditions)
        {
            foreach (var condition in conditions)
            {
                if (condition.SourceGroup - 1 == smartEvent.ActualId)
                {
                    Condition cond = new Condition();
                    cond.Type = condition.ConditionType;
                    cond.Target = condition.ConditionTarget;
                    cond.Value1 = condition.ConditionValue1;
                    cond.Value2 = condition.ConditionValue2;
                    cond.Value3 = condition.ConditionValue3;
                    cond.Negative = condition.NegativeCondition > 0 ? true : false;
                    smartEvent.AddCondition(condition.ElseGroup, cond);
                }
            }
        }
    }

}
