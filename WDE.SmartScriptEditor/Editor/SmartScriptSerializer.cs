using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Models.Helpers;

namespace WDE.SmartScriptEditor.Exporter
{
    public static class SmartScriptSerializer
    {
        public static AbstractSmartScriptLine ToMetaSmartScriptLine(this GlobalVariable gv, 
            int scriptEntryOrGuid,
            SmartScriptType scriptSourceType,
            int id)
        {
            var entry = gv.Entry == 0 ? "" : $" entry: {gv.Entry}";
            return new AbstractSmartScriptLine()
            {
                EntryOrGuid = scriptEntryOrGuid,
                ScriptSourceType = (int) scriptSourceType,
                Id = id,
                LineId = -1,
                EventType = SmartConstants.EventAiInitialize,
                ActionType = SmartConstants.ActionNone,
                Comment = string.IsNullOrEmpty(gv.Comment) ? $"#define {gv.VariableType} {gv.Key}{entry} {gv.Name}" : $"#define {gv.VariableType} {gv.Key} {gv.Name} -- {gv.Comment}"
            };
        }

        public static bool TryParseEndGroupComment(this string comment)
        {
            return comment.StartsWith(SmartConstants.EndGroupText);
        }
        
        public static bool TryParseBeginGroupComment(this string comment, out string header, out string? description)
        {
            description = null;
            header = "";
            
            if (!comment.StartsWith(SmartConstants.BeginGroupText))
                return false;

            var stringView = comment.AsSpan(SmartConstants.BeginGroupText.Length);
            if (comment.EndsWith(" - "))
                stringView  = stringView.Slice(0, stringView.Length - 3);

            var indexOfSeparator = stringView.IndexOf(SmartConstants.BeginGroupSeparator, StringComparison.Ordinal);
            var headerName = indexOfSeparator == -1
                ? stringView
                : stringView.Slice(0, indexOfSeparator);
            ReadOnlySpan<char> descriptionName = indexOfSeparator == -1
                ? null
                : stringView.Slice(indexOfSeparator + SmartConstants.BeginGroupSeparator.Length);

            header = headerName.ToString();
            if (!descriptionName.IsEmpty)
                description = descriptionName.ToString();
            return true;
        }

        public static AbstractSmartScriptLine[] ToSmartScriptLines(this SmartEvent e,
            uint? creatureEntry,
            int scriptEntryOrGuid,
            SmartScriptType scriptSourceType,
            int id,
            bool autoLinks = false,
            int? linkTo = null,
            bool shortComments = false)
        {
            var lines = new List<AbstractSmartScriptLine>();
            IList<SmartAction> actions = e.Actions.Count == 0
                ? new List<SmartAction>
                {
                    new(-1, e.EditorFeatures, new SmartSource(-1, e.EditorFeatures) {ReadableHint = ""}, new SmartTarget(-1, e.EditorFeatures) {ReadableHint = ""})
                    {
                        ReadableHint = "",
                        Parent = e
                    }
                }
                : e.Actions;

            for (var index = 0; index < actions.Count; index++)
            {
                if (autoLinks)
                    linkTo = index == actions.Count - 1 ? 0 : id + index + 1;
                
                SmartAction a = actions[index];
                AbstractSmartScriptLine line = new()
                {
                    CreatureEntry = creatureEntry,
                    EntryOrGuid = scriptEntryOrGuid,
                    ScriptSourceType = (int) scriptSourceType,
                    Id = id + (autoLinks ? index : 0),
                    Difficulties = default,
                    Link = linkTo ?? 0,
                    LineId = a.VirtualLineId,
                    EventType = e.Id,
                    EventPhaseMask = (int) e.Phases.Value,
                    EventChance = (int) e.Chance.Value,
                    EventFlags = (int) e.Flags.Value,
                    TimerId = (int)e.TimerId.Value,
                    EventParam1 = (int) e.GetValueOrDefault(0),
                    EventParam2 = (int) e.GetValueOrDefault(1),
                    EventParam3 = (int) e.GetValueOrDefault(2),
                    EventParam4 = (int) e.GetValueOrDefault(3),
                    EventParam5 = (int) e.GetValueOrDefault(4),
                    EventParam6 = (int) e.GetValueOrDefault(5),
                    EventParam7 = (int) e.GetValueOrDefault(6),
                    EventParam8 = (int) e.GetValueOrDefault(7),
                    EventFloatParam1 = e.GetFloatValueOrDefault(0),
                    EventFloatParam2 = e.GetFloatValueOrDefault(1),
                    EventStringParam = e.GetStringValueOrDefault(0) ?? "",
                    EventCooldownMin = (int) e.CooldownMin.Value,
                    EventCooldownMax = (int) e.CooldownMax.Value,
                    ActionType = a.Id,
                    ActionParam1 = (int) a.GetValueOrDefault(0),
                    ActionParam2 = (int) a.GetValueOrDefault(1),
                    ActionParam3 = (int) a.GetValueOrDefault(2),
                    ActionParam4 = (int) a.GetValueOrDefault(3),
                    ActionParam5 = (int) a.GetValueOrDefault(4),
                    ActionParam6 = (int) a.GetValueOrDefault(5),
                    ActionParam7 = (int) a.GetValueOrDefault(6),
                    ActionParam8 = (int) a.GetValueOrDefault(7),
                    ActionFloatParam1 = a.GetFloatValueOrDefault(0),
                    ActionFloatParam2 = a.GetFloatValueOrDefault(1),
                    SourceType = a.Source.Id,
                    SourceParam1 = (int) a.Source.GetValueOrDefault(0),
                    SourceParam2 = (int) a.Source.GetValueOrDefault(1),
                    SourceParam3 = (int) a.Source.GetValueOrDefault(2),
                    SourceX = a.Source.GetFloatValueOrDefault(0),
                    SourceY = a.Source.GetFloatValueOrDefault(1),
                    SourceZ = a.Source.GetFloatValueOrDefault(2),
                    SourceO = a.Source.GetFloatValueOrDefault(3),
                    SourceConditionId = (int)a.Source.Condition.Value,
                    TargetType = a.Target.Id,
                    TargetParam1 = (int) a.Target.GetValueOrDefault(0),
                    TargetParam2 = (int) a.Target.GetValueOrDefault(1),
                    TargetParam3 = (int) a.Target.GetValueOrDefault(2),
                    TargetConditionId = (int)a.Target.Condition.Value,
                    TargetX = a.Target.GetFloatValueOrDefault(0),
                    TargetY = a.Target.GetFloatValueOrDefault(1),
                    TargetZ = a.Target.GetFloatValueOrDefault(2),
                    TargetO = a.Target.GetFloatValueOrDefault(3),
                    Comment = shortComments ? a.Comment : e.Readable.RemoveTags().Trim() + " - " + a.Readable.RemoveTags().Trim() +
                              (string.IsNullOrEmpty(a.Comment) ? "" : " // " + a.Comment)
                };
                if (autoLinks && index > 0)
                {
                    line.EventType = SmartConstants.EventLink;
                    line.EventPhaseMask = 0;
                    line.EventChance = 100;
                    line.EventFlags = 0;
                    line.EventParam1 = 0;
                    line.EventParam2 = 0;
                    line.EventParam3 = 0;
                    line.EventParam4 = 0;
                    line.EventCooldownMin = 0;
                    line.EventCooldownMax = 0;
                }
                lines.Add(line);
            }

            return lines.ToArray();
        }
    }
}