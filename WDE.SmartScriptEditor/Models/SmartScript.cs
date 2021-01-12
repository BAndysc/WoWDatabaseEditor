using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using WDE.Common.Database;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartScript
    {
        public readonly int EntryOrGuid;
        public readonly ObservableCollection<SmartEvent> Events;
        private readonly ISmartFactory smartFactory;
        public readonly SmartScriptType SourceType;

        public SmartScript(SmartScriptSolutionItem item, ISmartFactory smartFactory)
        {
            EntryOrGuid = item.Entry;
            SourceType = item.SmartType;
            Events = new ObservableCollection<SmartEvent>();
            this.smartFactory = smartFactory;
        }

        public event Action BulkEditingStarted = delegate { };
        public event Action<string> BulkEditingFinished = delegate { };

        public void Load(IEnumerable<ISmartScriptLine> lines)
        {
            int? entry = null;
            SmartScriptType? source = null;
            int previousLink = -1;
            SmartEvent currentEvent = null;

            SortedDictionary<int, SmartEvent> triggerIdToActionParent = new();
            SortedDictionary<int, SmartEvent> triggerIdToEvent = new();

            foreach (ISmartScriptLine line in lines)
            {
                if (!entry.HasValue)
                    entry = line.EntryOrGuid;
                else
                    Debug.Assert(entry.Value == line.EntryOrGuid);

                if (!source.HasValue)
                    source = (SmartScriptType) line.ScriptSourceType;
                else
                    Debug.Assert((int) source.Value == line.ScriptSourceType);

                if (previousLink != line.Id)
                {
                    currentEvent = SafeEventFactory(line);
                    if (currentEvent != null)
                    {
                        if (currentEvent.Id == SmartConstants.EventTriggerTimed)
                            triggerIdToEvent[currentEvent.GetParameter(0).Value] = currentEvent;
                        Events.Add(currentEvent);
                    }
                    else
                        continue;
                }

                string comment = line.Comment.Contains(" // ") ? line.Comment.Substring(line.Comment.IndexOf(" // ") + 4).Trim() : "";

                SmartAction action = SafeActionFactory(line);
                if (action != null)
                {
                    if (comment != SmartConstants.CommentWait)
                        action.Comment = comment;
                    if (action.Id == SmartConstants.ActionTriggerTimed && comment == SmartConstants.CommentWait)
                        triggerIdToActionParent[action.GetParameter(0).Value] = currentEvent;
                    currentEvent.AddAction(action);
                }

                previousLink = line.Link;
            }

            var sortedTriggers = triggerIdToEvent.Keys.ToList();
            sortedTriggers.Reverse();
            foreach (int triggerId in sortedTriggers)
            {
                SmartEvent @event = triggerIdToEvent[triggerId];
                if (!triggerIdToActionParent.ContainsKey(triggerId))
                    continue;

                SmartEvent caller = triggerIdToActionParent[triggerId];

                SmartAction lastAction = caller.Actions[caller.Actions.Count - 1];

                if (lastAction.Id != SmartConstants.ActionTriggerTimed ||
                    lastAction.GetParameter(1).Value != lastAction.GetParameter(2).Value)
                    continue;

                int waitTime = lastAction.GetParameter(1).Value;
                SmartAction waitAction = smartFactory.ActionFactory(SmartConstants.ActionWait,
                    smartFactory.SourceFactory(SmartConstants.SourceNone),
                    smartFactory.TargetFactory(SmartConstants.TargetNone));
                waitAction.SetParameter(0, waitTime);

                caller.Actions.RemoveAt(caller.Actions.Count - 1);
                caller.AddAction(waitAction);
                foreach (SmartAction a in @event.Actions)
                    caller.AddAction(a);
                Events.Remove(@event);
            }
        }

        public void InsertFromClipboard(int index, IEnumerable<ISmartScriptLine> lines)
        {
            SmartEvent currentEvent = null;
            var prevIndex = 0;
            foreach (ISmartScriptLine line in lines)
            {
                if (currentEvent == null || prevIndex != line.Id)
                {
                    prevIndex = line.Id;
                    currentEvent = SafeEventFactory(line);
                    Events.Insert(index++, currentEvent);
                }

                if (line.ActionType != -1)
                {
                    SmartAction action = SafeActionFactory(line);
                    if (action != null)
                        currentEvent.AddAction(action);
                }
            }
        }

        public SmartAction SafeActionFactory(ISmartScriptLine line)
        {
            try
            {
                return smartFactory.ActionFactory(line);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Action {line.ActionType} unknown, skipping action");
            }

            return null;
        }

        public SmartEvent SafeEventFactory(ISmartScriptLine line)
        {
            try
            {
                return smartFactory.EventFactory(line);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Event {line.EventType} unknown, skipping action");
            }

            return null;
        }

        public IDisposable BulkEdit(string name)
        {
            return new BulkEditing(this, name);
        }

        private class BulkEditing : IDisposable
        {
            private readonly string name;
            private readonly SmartScript smartScript;

            public BulkEditing(SmartScript smartScript, string name)
            {
                this.smartScript = smartScript;
                this.name = name;
                this.smartScript.BulkEditingStarted.Invoke();
            }

            public void Dispose()
            {
                smartScript.BulkEditingFinished.Invoke(name);
            }
        }
    }
}