using System.Globalization;
using System.Linq;
using System.Text;
using SmartFormat;
using WDE.Common.Database;
using WDE.Conditions.Exporter;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Models;

namespace WDE.TrinitySmartScriptEditor.Exporter
{
    public class ExporterHelper
    {
        private static readonly string SaiSql =
            "({entryorguid}, {source_type}, {id}, {linkto}, {event_id}, {phasemask}, {chance}, {flags}, {event_param1}, {event_param2}, {event_param3}, {event_param4}, {action_id}, {action_param1}, {action_param2}, {action_param3}, {action_param4}, {action_param5}, {action_param6}, {target_id}, {target_param1}, {target_param2}, {target_param3}, {x}, {y}, {z}, {o}, \"{comment}\")";

        private readonly SmartScript script;
        private readonly ISmartScriptExporter scriptExporter;
        private readonly StringBuilder sql = new();

        public ExporterHelper(SmartScript script, ISmartScriptExporter scriptExporter)
        {
            this.script = script;
            this.scriptExporter = scriptExporter;
        }

        public string GetSql()
        {
            BuildHeader();
            return sql.ToString();
        }

        private void BuildHeader()
        {
            sql.AppendLine($"SET @ENTRY := {script.EntryOrGuid};");
            BuildDelete();
            BuildUpdate();
            BuildInsert();
        }

        private void BuildInsert()
        {
            var (serializedScript, serializedConditions) = scriptExporter.ToDatabaseCompatibleSmartScript(script);

            if (serializedScript.Length == 0)
                return;

            sql.AppendLine(
                "INSERT INTO smart_scripts (entryorguid, source_type, id, link, event_type, event_phase_mask, event_chance, event_flags, event_param1, event_param2, event_param3, event_param4, action_type, action_param1, action_param2, action_param3, action_param4, action_param5, action_param6, target_type, target_param1, target_param2, target_param3, target_x, target_y, target_z, target_o, comment) VALUES");

            var lines = serializedScript.Select(GenerateSingleSai);

            sql.Append(string.Join(",\n", lines));
            sql.AppendLine(";");
            sql.AppendLine();
            sql.AppendLine();

            var conditionsExporter = new ConditionsExporter(serializedConditions,
                new IDatabaseProvider.ConditionKey(SmartConstants.ConditionSourceSmartScript,
                    null,
                    script.EntryOrGuid,
                    (int) script.SourceType));

            sql.AppendLine(conditionsExporter.GetSql());
        }

        private string GenerateSingleSai(ISmartScriptLine line)
        {
            //if (action.Id == 188) // ACTION DEBUG MESSAGE
            //    comment = action.Comment;
            object data = new
            {
                entryorguid = "@ENTRY",
                source_type = ((int) script.SourceType).ToString(),
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

                action_source_id = line.SourceType.ToString(),
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

            return Smart.Format(SaiSql, data);
        }

        private void BuildUpdate()
        {
            switch (script.SourceType)
            {
                case SmartScriptType.Creature:
                    sql.AppendLine("UPDATE creature_template SET AIName=\"SmartAI\" WHERE entry= @ENTRY;");
                    break;
                case SmartScriptType.GameObject:
                    sql.AppendLine("UPDATE gameobject_template SET AIName=\"SmartGameObjectAI\" WHERE entry=@ENTRY;");
                    break;
                case SmartScriptType.Quest:
                    sql.AppendLine("INSERT IGNORE INTO quest_template_addon (ID) VALUES (@ENTRY);");
                    sql.AppendLine("UPDATE quest_template_addon SET ScriptName=\"SmartQuest\" WHERE ID=@ENTRY;");
                    break;
                case SmartScriptType.Spell:
                    sql.AppendLine("DELETE FROM spell_script_names WHERE spell_id = @ENTRY And ScriptName=\"SmartSpell\";");
                    sql.AppendLine("INSERT INTO spell_script_names(spell_id, ScriptName) VALUES(@ENTRY, \"SmartSpell\");");
                    break;
                case SmartScriptType.Aura:
                    sql.AppendLine("DELETE FROM spell_script_names WHERE spell_id = @ENTRY And ScriptName=\"SmartAura\";");
                    sql.AppendLine("INSERT INTO spell_script_names(spell_id, ScriptName) VALUES(@ENTRY, \"SmartAura\");");
                    break;
                case SmartScriptType.Cinematic:
                    sql.AppendLine("DELETE FROM cinematic_scripts WHERE cinematicId = @ENTRY;");
                    sql.AppendLine("INSERT INTO cinematic_scripts(cinematicId, ScriptName) VALUES(@ENTRY, \"SmartCinematic\");");
                    break;
                case SmartScriptType.AreaTrigger:
                    sql.AppendLine("DELETE FROM areatrigger_scripts WHERE entry = @ENTRY;");
                    sql.AppendLine("INSERT INTO areatrigger_scripts(entry, ScriptName) VALUES(@ENTRY, \"SmartTrigger\");");
                    break;
                case SmartScriptType.AreaTriggerEntityServerSide:
                    sql.AppendLine("UPDATE areatrigger_template SET ScriptName = \"SmartAreaTriggerAI\" WHERE Id = @ENTRY AND IsServerSide = 1;");
                    break;
                case SmartScriptType.AreaTriggerEntity:
                    sql.AppendLine("UPDATE areatrigger_template SET ScriptName = \"SmartAreaTriggerAI\" WHERE Id = @ENTRY AND IsServerSide = 0;");
                    break;
            }
        }

        private void BuildDelete()
        {
            sql.AppendLine(
                $"DELETE FROM smart_scripts WHERE entryOrGuid = {script.EntryOrGuid} AND source_type = {(int) script.SourceType};");
        }
    }
}