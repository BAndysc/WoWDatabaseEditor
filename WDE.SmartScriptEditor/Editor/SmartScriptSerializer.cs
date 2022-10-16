using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using SmartFormat;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Exporter
{
    public static class SmartScriptSerializer
    {
        private static readonly Regex SaiLineRegex = new(
            @"\(\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?),\s*""(.*)""\s*\)");

        private static readonly string SerializedStringFormat =
            "({entryorguid}, {source_type}, {id}, {linkto}, {event_id}, {phasemask}, {chance}, {flags}, {event_param1}, {event_param2}, {event_param3}, {event_param4}, {action_id}, {action_param1}, {action_param2}, {action_param3}, {action_param4}, {action_param5}, {action_param6}, {source_id}, {source_param1}, {source_param2}, {source_param3}, {target_id}, {target_param1}, {target_param2}, {target_param3}, {x}, {y}, {z}, {o}, \"{comment}\")";

        public static bool TryToISmartScriptLine(this string str, out ISmartScriptLine line)
        {
            line = new AbstractSmartScriptLine();

            Match m = SaiLineRegex.Match(str);
            if (!m.Success || m.Groups.Count != 33)
                return false;

            line.EntryOrGuid = int.Parse(m.Groups[1].ToString());
            line.ScriptSourceType = int.Parse(m.Groups[2].ToString());
            line.Id = int.Parse(m.Groups[3].ToString());
            line.Link = int.Parse(m.Groups[4].ToString());
            line.EventType = int.Parse(m.Groups[5].ToString());
            line.EventPhaseMask = int.Parse(m.Groups[6].ToString());
            line.EventChance = int.Parse(m.Groups[7].ToString());
            line.EventFlags = int.Parse(m.Groups[8].ToString());
            line.EventParam1 = int.Parse(m.Groups[9].ToString());
            line.EventParam2 = int.Parse(m.Groups[10].ToString());
            line.EventParam3 = int.Parse(m.Groups[11].ToString());
            line.EventParam4 = int.Parse(m.Groups[12].ToString());
            line.ActionType = int.Parse(m.Groups[13].ToString());
            line.ActionParam1 = int.Parse(m.Groups[14].ToString());
            line.ActionParam2 = int.Parse(m.Groups[15].ToString());
            line.ActionParam3 = int.Parse(m.Groups[16].ToString());
            line.ActionParam4 = int.Parse(m.Groups[17].ToString());
            line.ActionParam5 = int.Parse(m.Groups[18].ToString());
            line.ActionParam6 = int.Parse(m.Groups[19].ToString());
            line.SourceType = int.Parse(m.Groups[20].ToString());
            line.SourceParam1 = int.Parse(m.Groups[21].ToString());
            line.SourceParam2 = int.Parse(m.Groups[22].ToString());
            line.SourceParam3 = int.Parse(m.Groups[23].ToString());
            line.TargetType = int.Parse(m.Groups[24].ToString());
            line.TargetParam1 = int.Parse(m.Groups[25].ToString());
            line.TargetParam2 = int.Parse(m.Groups[26].ToString());
            line.TargetParam3 = int.Parse(m.Groups[27].ToString());
            line.TargetX = float.Parse(m.Groups[28].ToString(), CultureInfo.InvariantCulture);
            line.TargetY = float.Parse(m.Groups[29].ToString(), CultureInfo.InvariantCulture);
            line.TargetZ = float.Parse(m.Groups[30].ToString(), CultureInfo.InvariantCulture);
            line.TargetO = float.Parse(m.Groups[31].ToString(), CultureInfo.InvariantCulture);
            line.Comment = m.Groups[32].ToString();

            return true;
        }

        public static string SerializeToString(this ISmartScriptLine line)
        {
            object data = new
            {
                entryorguid = line.EntryOrGuid.ToString(),
                source_type = line.SourceType.ToString(),
                id = line.Id.ToString(),
                linkto = line.Link.ToString(),

                event_id = line.EventType.ToString(),
                phasemask = line.EventPhaseMask.ToString(),
                chance = line.EventChance.ToString(),
                flags = line.EventFlags.ToString(),
                event_param1 = line.EventParam1.ToString(),
                event_param2 = line.EventParam2.ToString(),
                event_param3 = line.EventParam3.ToString(),
                event_param4 = line.EventParam4.ToString(),

                event_cooldown_min = line.EventCooldownMin.ToString(),
                event_cooldown_max = line.EventCooldownMax.ToString(),

                action_id = line.ActionType.ToString(),
                action_param1 = line.ActionParam1.ToString(),
                action_param2 = line.ActionParam2.ToString(),
                action_param3 = line.ActionParam3.ToString(),
                action_param4 = line.ActionParam4.ToString(),
                action_param5 = line.ActionParam5.ToString(),
                action_param6 = line.ActionParam6.ToString(),

                source_id = line.SourceType.ToString(),
                source_param1 = line.SourceParam1.ToString(),
                source_param2 = line.SourceParam2.ToString(),
                source_param3 = line.SourceParam3.ToString(),
                source_condition_id = line.SourceConditionId.ToString(),

                target_id = line.TargetType.ToString(),
                target_param1 = line.TargetParam1.ToString(),
                target_param2 = line.TargetParam2.ToString(),
                target_param3 = line.TargetParam3.ToString(),
                target_condition_id = line.TargetConditionId.ToString(),

                x = line.TargetX.ToString(CultureInfo.InvariantCulture),
                y = line.TargetY.ToString(CultureInfo.InvariantCulture),
                z = line.TargetZ.ToString(CultureInfo.InvariantCulture),
                o = line.TargetO.ToString(CultureInfo.InvariantCulture),

                comment = line.Comment
            };

            return Smart.Format(SerializedStringFormat, data);
        }

        public static ISmartScriptLine ToMetaSmartScriptLine(this GlobalVariable gv, 
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

        public static ISmartScriptLine[] ToSmartScriptLines(this SmartEvent e,
            int scriptEntryOrGuid,
            SmartScriptType scriptSourceType,
            int id,
            bool autoLinks = false,
            int? linkTo = null,
            bool shortComments = false)
        {
            var lines = new List<ISmartScriptLine>();
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
        
        public static IConditionLine[] ToConditionLines(this SmartEvent e,
            int conditionSourceType,
            int scriptEntryOrGuid,
            SmartScriptType scriptSourceType,
            int id)
        {
            var lines = new List<IConditionLine>();
            var elseGroup = 0;

            for (var index = 0; index < e.Conditions.Count; index++)
            {
                SmartCondition c = e.Conditions[index];
                if (c.Id == SmartConstants.ConditionOr)
                {
                    elseGroup++;
                    continue;
                }
                lines.Add(new AbstractConditionLine()
                {
                    SourceType = conditionSourceType,
                    SourceGroup = id + 1,
                    SourceEntry = scriptEntryOrGuid,
                    SourceId = (int)scriptSourceType,
                    ElseGroup = elseGroup,
                    ConditionType = c.Id,
                    ConditionTarget = (byte)c.ConditionTarget.Value,
                    ConditionValue1 = (int)c.GetParameter(0).Value,
                    ConditionValue2 = (int)c.GetParameter(1).Value,
                    ConditionValue3 = (int)c.GetParameter(2).Value,
                    NegativeCondition = (int)c.Inverted.Value,
                    Comment = c.Readable.RemoveTags()
                });
            }
            return lines.ToArray();
        }
    }
}