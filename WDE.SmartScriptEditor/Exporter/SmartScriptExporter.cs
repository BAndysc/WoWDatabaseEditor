using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WDE.Common.Database;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;
using Prism.Ioc;

namespace WDE.SmartScriptEditor.Exporter
{
    public class SmartScriptExporter
    {
        private static readonly string SAI_SQL = "({entryorguid}, {source_type}, {id}, {linkto}, {event_id}, {phasemask}, {chance}, {flags}, {event_param1}, {event_param2}, {event_param3}, {event_param4}, {event_cooldown_min}, {event_cooldown_max}, {action_id}, {action_param1}, {action_param2}, {action_param3}, {action_param4}, {action_param5}, {action_param6}, {action_source_id}, {source_param1}, {source_param2}, {source_param3}, {source_condition_id}, {target_id}, {target_param1}, {target_param2}, {target_param3}, {target_condition_id}, {x}, {y}, {z}, {o}, \"{comment}\")";

        private readonly SmartScript _script;
        private readonly ISmartFactory smartFactory;
        private StringBuilder _sql = new StringBuilder();

        public SmartScriptExporter(SmartScript script, ISmartFactory smartFactory)
        {
            _script = script;
            this.smartFactory = smartFactory;
        }

        public string GetSql()
        {
            BuildHeader();
            return _sql.ToString();
        }

        private void BuildHeader()
        {
            _sql.AppendLine($"SET @ENTRY := {_script.EntryOrGuid};");
            BuildDelete();
            BuildUpdate();
            BuildInsert();
        }

        private void BuildInsert()
        {
            if (_script.Events.Count == 0)
                return;

            _sql.AppendLine(
                "INSERT INTO smart_scripts (entryorguid, script_source_type, id, link, event_type, event_phase_mask, event_chance, event_flags, event_param1, event_param2, event_param3, event_param4, event_cooldown_min, event_cooldown_max, action_type, action_param1, action_param2, action_param3, action_param4, action_param5, action_param6, source_type, source_param1, source_param2, source_param3, source_condition_id, target_type, target_param1, target_param2, target_param3, target_condition_id, target_x, target_y, target_z, target_o, comment) VALUES");

            int eventId = 0;
            List<string> lines = new List<string>();

            foreach (SmartEvent e in _script.Events)
            {
                if (e.Actions.Count == 0)
                    continue;

                e.ActualId = eventId;
                lines.Add(GenerateSingleSai(eventId, e, e.Actions[0], (e.Actions.Count == 1 ? 0 : eventId + 1)));

                eventId++;

                for (int index = 1; index < e.Actions.Count; ++index)
                {
                    lines.Add(GenerateSingleSai(eventId, smartFactory.EventFactory(61),
                        e.Actions[index], (e.Actions.Count - 1 == index ? 0 : eventId + 1)));
                    eventId++;
                }
            }
            _sql.Append(string.Join(",\n", lines));
            _sql.AppendLine(";");
        }

        private string GenerateSingleSai(int eventId, SmartEvent ev, SmartAction action, int link = 0, string comment = null)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            //if (action.Id == 188) // ACTION DEBUG MESSAGE
            //    comment = action.Comment;
            object data = new
            {
                entryorguid = "@ENTRY",
                source_type = ((int)_script.SourceType).ToString(),
                id = eventId.ToString(),
                linkto = link.ToString(),

                event_id = ev.Id.ToString(),
                phasemask = ev.Phases.GetValue().ToString(),
                chance = ev.Chance.ToString(),
                flags = ev.Flags.GetValue().ToString(),
                event_param1 = ev.GetParameter(0).GetValue().ToString(),
                event_param2 = ev.GetParameter(1).GetValue().ToString(),
                event_param3 = ev.GetParameter(2).GetValue().ToString(),
                event_param4 = ev.GetParameter(3).GetValue().ToString(),

                event_cooldown_min = ev.CooldownMin.GetValue().ToString(),
                event_cooldown_max = ev.CooldownMax.GetValue().ToString(),

                action_id = action.Id.ToString(),
                action_param1 = action.GetParameter(0).GetValue().ToString(),
                action_param2 = action.GetParameter(1).GetValue().ToString(),
                action_param3 = action.GetParameter(2).GetValue().ToString(),
                action_param4 = action.GetParameter(3).GetValue().ToString(),
                action_param5 = action.GetParameter(4).GetValue().ToString(),
                action_param6 = action.GetParameter(5).GetValue().ToString(),

                action_source_id = action.Source.Id.ToString(),
                source_param1 = action.Source.GetParameter(0).GetValue().ToString(),
                source_param2 = action.Source.GetParameter(1).GetValue().ToString(),
                source_param3 = action.Source.GetParameter(2).GetValue().ToString(),
                source_condition_id = action.Source.Condition.GetValue().ToString(),

                target_id = action.Target.Id.ToString(),
                target_param1 = action.Target.GetParameter(0).GetValue().ToString(),
                target_param2 = action.Target.GetParameter(1).GetValue().ToString(),
                target_param3 = action.Target.GetParameter(2).GetValue().ToString(),
                target_condition_id = action.Target.Condition.GetValue().ToString(),


                x = action.Target.X.ToString(),
                y = action.Target.X.ToString(),
                z = action.Target.X.ToString(),
                o = action.Target.X.ToString(),

                comment = comment ?? (ev.Readable + " - " + action.Readable)
            };

            return SmartFormat.Smart.Format(SAI_SQL, data);
        }

        private void BuildUpdate()
        {
            switch (_script.SourceType)
            {
                case SmartScriptType.Creature:
                    _sql.AppendLine(
                        "UPDATE creature_template SET AIName=\"SmartAI\" WHERE entry= @ENTRY;");
                    break;
                case SmartScriptType.GameObject:
                    _sql.AppendLine(
                        "UPDATE gameobject_template SET AIName=\"SmartGameObjectAI\" WHERE entry=@ENTRY;");
                    break;
                case SmartScriptType.Quest:
                    _sql.AppendLine("DELETE FROM quest_scripts WHERE questId = @ENTRY;");
                    _sql.AppendLine("INSERT INTO quest_scripts(questId, ScriptName) VALUES(@ENTRY, \"SmartQuest\");");
                    break;
                case SmartScriptType.Spell:
                    _sql.AppendLine("DELETE FROM spell_script_names WHERE spell_id = @ENTRY And ScriptName=\"SmartSpell\";");
                    _sql.AppendLine("INSERT INTO spell_script_names(spell_id, ScriptName) VALUES(@ENTRY, \"SmartSpell\");");
                    break;
                case SmartScriptType.Aura:
                    _sql.AppendLine("DELETE FROM spell_script_names WHERE spell_id = @ENTRY And ScriptName=\"SmartAura\";");
                    _sql.AppendLine("INSERT INTO spell_script_names(spell_id, ScriptName) VALUES(@ENTRY, \"SmartAura\");");
                    break;
                case SmartScriptType.Cinematic:
                    _sql.AppendLine("DELETE FROM cinematic_scripts WHERE cinematicId = @ENTRY;");
                    _sql.AppendLine("INSERT INTO cinematic_scripts(cinematicId, ScriptName) VALUES(@ENTRY, \"SmartCinematic\");");
                    break;
                case SmartScriptType.AreaTrigger:
                    _sql.AppendLine("DELETE FROM areatrigger_scripts WHERE entry = @ENTRY;");
                    _sql.AppendLine("INSERT INTO areatrigger_scripts(entry, ScriptName) VALUES(@ENTRY, \"SmartTrigger\");");
                    break;
            }
        }

        private void BuildDelete()
        {
            _sql.AppendLine(
                $"DELETE FROM smart_scripts WHERE entryOrGuid = {_script.EntryOrGuid} AND script_source_type = {(int)_script.SourceType};");
        }
    }
}
