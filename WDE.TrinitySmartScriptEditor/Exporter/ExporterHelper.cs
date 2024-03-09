using System;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Models;
using WDE.SqlQueryGenerator;

namespace WDE.TrinitySmartScriptEditor.Exporter
{
    public class ExporterHelper
    {
        private readonly SmartScript script;
        private readonly IDatabaseProvider databaseProvider;
        private readonly ISmartScriptSolutionItem item;
        private readonly ISmartScriptExporter scriptExporter;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly IConditionQueryGenerator conditionQueryGenerator;
        private readonly ISolutionItemNameRegistry nameProvider;

        private DatabaseTable SmartScriptTableName => currentCoreVersion.Current.SmartScriptFeatures.TableName;
        
        public ExporterHelper(SmartScript script, 
            IDatabaseProvider databaseProvider,
            ISmartScriptSolutionItem item,
            ISmartScriptExporter scriptExporter,
            ICurrentCoreVersion currentCoreVersion,
            ISolutionItemNameRegistry nameProvider,
            IConditionQueryGenerator conditionQueryGenerator)
        {
            this.script = script;
            this.databaseProvider = databaseProvider;
            this.item = item;
            this.scriptExporter = scriptExporter;
            this.currentCoreVersion = currentCoreVersion;
            this.conditionQueryGenerator = conditionQueryGenerator;
            this.nameProvider = nameProvider;
        }

        public async Task<IQuery> GetSql()
        {
            var query = Queries.BeginTransaction(DataDatabaseType.World);
            query.Comment(nameProvider.GetName(item));
            query.DefineVariable("ENTRY", script.EntryOrGuid);
            var (serializedScript, serializedConditions) = scriptExporter.ToDatabaseCompatibleSmartScript(script);
            await BuildUpdate(query);
            BuildDelete(query, serializedScript);
            BuildInsert(query, serializedScript, serializedConditions ?? Array.Empty<IConditionLine>());
            return query.Close();
        }
        
        private void BuildInsert(IMultiQuery query, ISmartScriptLine[] serializedScript, IConditionLine[] serializedConditions)
        {
            if (serializedScript.Length == 0)
                return;

            var lines = serializedScript.Select(s => GenerateSingleSai(query, s));

            query.Table(SmartScriptTableName).BulkInsert(lines);

            query.BlankLine();
            
            query.Add(conditionQueryGenerator.BuildDeleteQuery(new IDatabaseProvider.ConditionKey(
                SmartConstants.ConditionSourceSmartScript,
                null,
                script.EntryOrGuid,
                (int) script.SourceType)));
            query.Add(conditionQueryGenerator.BuildInsertQuery(serializedConditions));
        }

        private object GenerateSingleSai(IMultiQuery query, ISmartScriptLine line)
        {
            IRawText entryOrGuid = query.Raw("@ENTRY");
            if (line.EntryOrGuid != script.EntryOrGuid)
            {
                if (line.EntryOrGuid / 100 == script.EntryOrGuid)
                {
                    string plus = "";
                    if (line.EntryOrGuid % 100 != 0)
                        plus = " + " + (line.EntryOrGuid % 100);
                    entryOrGuid = query.Raw("@ENTRY * 100" + plus);
                }
                else
                    entryOrGuid = query.Raw(line.EntryOrGuid.ToString());
            }

            var difficulties = line.Difficulties.HasValue ? string.Join(",", line.Difficulties) : "";
            if (currentCoreVersion.Current.SmartScriptFeatures.DifficultyInSeparateColumn)
            {
                return new
                {
                    entryorguid = entryOrGuid,
                    source_type = (int) line.ScriptSourceType,
                    id = line.Id,
                    link = line.Link,
                    Difficulties = difficulties,

                    event_type = line.EventType,
                    event_phase_mask = line.EventPhaseMask,
                    event_chance = line.EventChance,
                    event_flags = line.EventFlags,
                    event_param1 = line.EventParam1,
                    event_param2 = line.EventParam2,
                    event_param3 = line.EventParam3,
                    event_param4 = line.EventParam4,

                    action_type = line.ActionType,
                    action_param1 = line.ActionParam1,
                    action_param2 = line.ActionParam2,
                    action_param3 = line.ActionParam3,
                    action_param4 = line.ActionParam4,
                    action_param5 = line.ActionParam5,
                    action_param6 = line.ActionParam6,

                    target_type = line.TargetType,
                    target_param1 = line.TargetParam1,
                    target_param2 = line.TargetParam2,
                    target_param3 = line.TargetParam3,

                    target_x = line.TargetX,
                    target_y = line.TargetY,
                    target_z = line.TargetZ,
                    target_o = line.TargetO,

                    comment = line.Comment
                };
            }
            else
            {
                return new
                {
                    entryorguid = entryOrGuid,
                    source_type = (int) line.ScriptSourceType,
                    id = line.Id,
                    link = line.Link,

                    event_type = line.EventType,
                    event_phase_mask = line.EventPhaseMask,
                    event_chance = line.EventChance,
                    event_flags = line.EventFlags,
                    event_param1 = line.EventParam1,
                    event_param2 = line.EventParam2,
                    event_param3 = line.EventParam3,
                    event_param4 = line.EventParam4,

                    action_type = line.ActionType,
                    action_param1 = line.ActionParam1,
                    action_param2 = line.ActionParam2,
                    action_param3 = line.ActionParam3,
                    action_param4 = line.ActionParam4,
                    action_param5 = line.ActionParam5,
                    action_param6 = line.ActionParam6,

                    target_type = line.TargetType,
                    target_param1 = line.TargetParam1,
                    target_param2 = line.TargetParam2,
                    target_param3 = line.TargetParam3,

                    target_x = line.TargetX,
                    target_y = line.TargetY,
                    target_z = line.TargetZ,
                    target_o = line.TargetO,

                    comment = line.Comment
                };   
            }
        }

        private async Task BuildUpdate(IMultiQuery query)
        {
            switch (script.SourceType)
            {
                case SmartScriptType.Creature:
                {
                    uint? entry;
                    if (script.Entry.HasValue)
                        entry = script.Entry.Value;
                    else if (script.EntryOrGuid >= 0)
                        entry = (uint)script.EntryOrGuid;
                    else
                        entry = (await databaseProvider.GetCreatureByGuidAsync(0, (uint)-script.EntryOrGuid))?.Entry;

                    if (entry.HasValue)
                    {
                        var condition = query
                            .Table(DatabaseTable.WorldTable("creature_template"))
                            .Where(t => t.Column<int>("entry") == t.Variable<int>("ENTRY"));
                        
                        if (script.EntryOrGuid != entry.Value)
                            condition = query
                                .Table(DatabaseTable.WorldTable("creature_template"))
                                .Where(t => t.Column<uint>("entry") == entry.Value);
                        
                        var update = condition
                            .Set("AIName", currentCoreVersion.Current.SmartScriptFeatures.CreatureSmartAiName)
                            .Set("ScriptName", "");
                        if (currentCoreVersion.Current.DatabaseFeatures.HasAiEntry)
                            update = update.Set("AIEntry", 0);
                        update.Update();
                    }
                    else
                        query.Comment("[WARNING] cannot set creature AI to SmartAI, because guid not found in `creature` table!");

                    break;
                }
                case SmartScriptType.GameObject:
                {
                    uint? entry;
                    if (script.Entry.HasValue)
                        entry = script.Entry.Value;
                    else if (script.EntryOrGuid >= 0)
                        entry = (uint)script.EntryOrGuid;
                    else
                        entry = (await databaseProvider.GetGameObjectByGuidAsync(0, (uint)-script.EntryOrGuid))?.Entry;

                    if (entry.HasValue)
                    {
                        var condition = query
                            .Table(DatabaseTable.WorldTable("gameobject_template"))
                            .Where(t => t.Column<int>("entry") == t.Variable<int>("ENTRY"));
                        
                        if (script.EntryOrGuid != entry.Value)
                            condition = query
                                .Table(DatabaseTable.WorldTable("gameobject_template"))
                                .Where(t => t.Column<uint>("entry") == entry.Value);
                        condition
                            .Set("AIName", currentCoreVersion.Current.SmartScriptFeatures.GameObjectSmartAiName)
                            .Update();
                    }
                    else
                        query.Comment("[WARNING] cannot set gameobject AI to SmartGameObjectAI, because guid not found in `gameobject` table!");
                    break;
                }
                case SmartScriptType.Quest:
                    query.Table(DatabaseTable.WorldTable("quest_template_addon"))
                        .InsertIgnore(new
                        {
                            ID = query.Variable("ENTRY")
                        });
                    query.Table(DatabaseTable.WorldTable("quest_template_addon"))
                        .Where(r => r.Column<int>("ID") == r.Variable<int>("ENTRY"))
                        .Set("ScriptName", "SmartQuest")
                        .Update();
                    break;
                case SmartScriptType.Spell:
                    query.Comment("TrinityCore doesn't support Smart Spell Script");
                    break;
                case SmartScriptType.Aura:
                    query.Comment("TrinityCore doesn't support Smart Aura Script");
                    break;
                case SmartScriptType.Cinematic:
                    query.Comment("TrinityCore doesn't support Smart Cinematic Script");
                    break;
                case SmartScriptType.AreaTrigger:
                    query.Table(DatabaseTable.WorldTable("areatrigger_scripts"))
                        .Where(r => r.Column<int>("entry") == r.Variable<int>("ENTRY"))
                        .Delete();
                    query.Table(DatabaseTable.WorldTable("areatrigger_scripts"))
                        .Insert(new
                        {
                            entry = query.Variable("ENTRY"),
                            ScriptName = "SmartTrigger"
                        });
                    break;
                case SmartScriptType.AreaTriggerEntityServerSide:
                    query.Table(DatabaseTable.WorldTable("areatrigger_template"))
                        .Where(r => r.Column<int>("Id") == r.Variable<int>("ENTRY") && r.Column<bool>("IsServerSide"))
                        .Set("ScriptName", "SmartAreaTriggerAI")
                        .Update();
                    break;
                case SmartScriptType.AreaTriggerEntity:
                    query.Table(DatabaseTable.WorldTable("areatrigger_template"))
                        .Where(r => r.Column<int>("Id") == r.Variable<int>("ENTRY") && !r.Column<bool>("IsServerSide"))
                        .Set("ScriptName", "SmartAreaTriggerAI")
                        .Update();
                    break;
                case SmartScriptType.Scene:
                    query.Table(DatabaseTable.WorldTable("scene_template"))
                        .Where(r => r.Column<int>("SceneId") == r.Variable<int>("ENTRY"))
                        .Set("ScriptName", "SmartScene")
                        .Update();
                    break;
            }
        }

        private void BuildDelete(IMultiQuery query, ISmartScriptLine[] serializedLines)
        {
            foreach (var pair in serializedLines.Select(l => (l.ScriptSourceType, l.EntryOrGuid))
                .Concat(new (int ScriptSourceType, int EntryOrGuid)[]{((int)script.SourceType, script.EntryOrGuid)})
                .Distinct()
                .GroupBy(pair => pair.ScriptSourceType))
            {
                var entries = pair.Select(p => p.EntryOrGuid).ToList();
                if (entries.Count == 1 && script.EntryOrGuid == entries[0])
                {
                    query.Table(SmartScriptTableName)
                        .Where(r => r.Column<int>("source_type") == pair.Key && 
                                    r.Column<int>("entryOrGuid") == r.Variable<int>("ENTRY")).Delete();   
                }
                else
                {
                    query.Table(SmartScriptTableName)
                        .Where(r => r.Column<int>("source_type") == pair.Key)
                        .WhereIn("entryOrGuid", entries, true).Delete();   
                }
            }
        }
    }
}
