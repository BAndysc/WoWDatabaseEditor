using System;
using System.Collections.Generic;
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
        private readonly IEditorFeatures editorFeatures;

        private DatabaseTable SmartScriptTableName => currentCoreVersion.Current.SmartScriptFeatures.TableName;
        
        public ExporterHelper(SmartScript script, 
            IDatabaseProvider databaseProvider,
            ISmartScriptSolutionItem item,
            ISmartScriptExporter scriptExporter,
            ICurrentCoreVersion currentCoreVersion,
            ISolutionItemNameRegistry nameProvider,
            IEditorFeatures editorFeatures,
            IConditionQueryGenerator conditionQueryGenerator)
        {
            this.script = script;
            this.databaseProvider = databaseProvider;
            this.item = item;
            this.scriptExporter = scriptExporter;
            this.currentCoreVersion = currentCoreVersion;
            this.conditionQueryGenerator = conditionQueryGenerator;
            this.nameProvider = nameProvider;
            this.editorFeatures = editorFeatures;
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

            var lines = serializedScript.Select(s => GenerateSingleSai(query, s)).ToList();

            query.Table(SmartScriptTableName).BulkInsert(lines);

            query.BlankLine();
            
            query.Add(conditionQueryGenerator.BuildDeleteQuery(new IDatabaseProvider.ConditionKey(
                SmartConstants.ConditionSourceSmartScript,
                null,
                script.EntryOrGuid,
                (int) script.SourceType)));
            query.Add(conditionQueryGenerator.BuildInsertQuery(serializedConditions));
        }

        private Dictionary<string, object?> GenerateSingleSai(IMultiQuery query, ISmartScriptLine line)
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
            Dictionary<string, object?> columns = new();

            columns["entryorguid"] = entryOrGuid;
            columns["source_type"] = (int) line.ScriptSourceType;
            columns["id"] = line.Id;
            columns["link"] = line.Link;

            columns["event_type"] = line.EventType;
            columns["event_phase_mask"] = line.EventPhaseMask;
            columns["event_chance"] = line.EventChance;
            columns["event_flags"] = line.EventFlags;
            for (int i = 0; i < editorFeatures.EventParametersCount.IntCount; ++i)
                columns[$"event_param{i+1}"] = (uint)line.GetEventParam(i);

            columns["action_type"] = line.ActionType;
            for (int i = 0; i < editorFeatures.ActionParametersCount.IntCount; ++i)
                columns[$"action_param{i+1}"] = (uint)line.GetActionParam(i);

            columns["target_type"] = line.TargetType;
            for (int i = 0; i < editorFeatures.TargetParametersCount.IntCount; ++i)
                columns[$"target_param{i+1}"] = (uint)line.GetTargetParam(i);

            columns["target_x"] = line.TargetX;
            columns["target_y"] = line.TargetY;
            columns["target_z"] = line.TargetZ;
            columns["target_o"] = line.TargetO;

            columns["comment"] = line.Comment;

            if (currentCoreVersion.Current.SmartScriptFeatures.DifficultyInSeparateColumn)
                columns["Difficulties"] = difficulties;

            return columns;
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
