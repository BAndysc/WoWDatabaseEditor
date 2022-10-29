using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Models;

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

        public static AbstractSmartScriptLine[] ToSmartScriptLines(this SmartEvent e,
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
                    EntryOrGuid = scriptEntryOrGuid,
                    ScriptSourceType = (int) scriptSourceType,
                    Id = id + (autoLinks ? index : 0),
                    Link = linkTo ?? 0,
                    LineId = a.LineId,
                    EventType = e.Id,
                    EventPhaseMask = (int) e.Phases.Value,
                    EventChance = (int) e.Chance.Value,
                    EventFlags = (int) e.Flags.Value,
                    TimerId = (int)e.TimerId.Value,
                    EventParam1 = (int) e.GetParameter(0).Value,
                    EventParam2 = (int) e.GetParameter(1).Value,
                    EventParam3 = (int) e.GetParameter(2).Value,
                    EventParam4 = (int) e.GetParameter(3).Value,
                    EventParam5 = (int) (e.ParametersCount >= 5 ? e.GetParameter(4).Value : 0),
                    EventFloatParam1 = e.FloatParametersCount >= 1 ? e.GetFloatParameter(0).Value : 0,
                    EventFloatParam2 = e.FloatParametersCount >= 2 ? e.GetFloatParameter(1).Value : 0,
                    EventStringParam = e.StringParametersCount >= 1 ? e.GetStringParameter(0).Value : "",
                    EventCooldownMin = (int) e.CooldownMin.Value,
                    EventCooldownMax = (int) e.CooldownMax.Value,
                    ActionType = a.Id,
                    ActionParam1 = (int) a.GetParameter(0).Value,
                    ActionParam2 = (int) a.GetParameter(1).Value,
                    ActionParam3 = (int) a.GetParameter(2).Value,
                    ActionParam4 = (int) a.GetParameter(3).Value,
                    ActionParam5 = (int) a.GetParameter(4).Value,
                    ActionParam6 = (int) a.GetParameter(5).Value,
                    ActionParam7 = (int) (a.ParametersCount >= 7 ? a.GetParameter(6).Value : 0),
                    ActionFloatParam1 = a.FloatParametersCount >= 1 ? a.GetFloatParameter(0).Value : 0,
                    ActionFloatParam2 = a.FloatParametersCount >= 2 ? a.GetFloatParameter(1).Value : 0,
                    SourceType = a.Source.Id,
                    SourceParam1 = (int) a.Source.GetParameter(0).Value,
                    SourceParam2 = (int) a.Source.GetParameter(1).Value,
                    SourceParam3 = (int) a.Source.GetParameter(2).Value,
                    SourceX = a.Source.FloatParametersCount >= 1 ? a.Source.X : 0,
                    SourceY = a.Source.FloatParametersCount >= 2 ? a.Source.Y : 0,
                    SourceZ = a.Source.FloatParametersCount >= 3 ? a.Source.Z : 0,
                    SourceO = a.Source.FloatParametersCount >= 4 ? a.Source.O : 0,
                    SourceConditionId = (int)a.Source.Condition.Value,
                    TargetType = a.Target.Id,
                    TargetParam1 = (int) a.Target.GetParameter(0).Value,
                    TargetParam2 = (int) a.Target.GetParameter(1).Value,
                    TargetParam3 = (int) a.Target.GetParameter(2).Value,
                    TargetConditionId = (int)a.Target.Condition.Value,
                    TargetX = a.Target.X,
                    TargetY = a.Target.Y,
                    TargetZ = a.Target.Z,
                    TargetO = a.Target.O,
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