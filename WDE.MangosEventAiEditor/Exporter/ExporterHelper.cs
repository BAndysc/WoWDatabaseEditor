using System.Linq;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.EventAiEditor.Editor.UserControls;
using WDE.EventAiEditor.Models;
using WDE.SqlQueryGenerator;

namespace WDE.MangosEventAiEditor.Exporter
{
    public class ExporterHelper
    {
        private readonly EventAiScript script;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IEventAiSolutionItem item;
        private readonly IEventAiExporter scriptExporter;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly ISolutionItemNameRegistry nameProvider;

        private DatabaseTable EventAiTableName => DatabaseTable.WorldTable("creature_ai_scripts");
        
        public ExporterHelper(EventAiScript script, 
            IDatabaseProvider databaseProvider,
            IEventAiSolutionItem item,
            IEventAiExporter scriptExporter,
            ICurrentCoreVersion currentCoreVersion,
            ISolutionItemNameRegistry nameProvider)
        {
            this.script = script;
            this.databaseProvider = databaseProvider;
            this.item = item;
            this.scriptExporter = scriptExporter;
            this.currentCoreVersion = currentCoreVersion;
            this.nameProvider = nameProvider;
        }

        public async Task<IQuery> GetSql()
        {
            var query = Queries.BeginTransaction(DataDatabaseType.World);
            query.Comment(nameProvider.GetName(item));
            var serializedScript = scriptExporter.ToDatabaseCompatibleEventAi(script);
            BuildDelete(query, item.EntryOrGuid, serializedScript);
            await BuildUpdate(query);
            BuildInsert(query, serializedScript);
            return query.Close();
        }
        
        private void BuildInsert(IMultiQuery query, IEventAiLine[] serializedScript)
        {
            if (serializedScript.Length == 0)
                return;

            var lines = serializedScript.Select(s => GenerateSingleEventAi(query, s));

            query.Table(EventAiTableName).BulkInsert(lines);
        }

        private object GenerateSingleEventAi(IMultiQuery query, IEventAiLine line)
        {
            return new
            {
                id = line.Id,	
                creature_id = line.CreatureIdOrGuid,
                event_type = line.EventType,
                event_inverse_phase_mask = line.EventInversePhaseMask,
                event_chance = line.EventChance,
                event_flags = line.EventFlags,
                event_param1 = line.EventParam1,
                event_param2 = line.EventParam2,
                event_param3 = line.EventParam3,
                event_param4 = line.EventParam4,
                event_param5 = line.EventParam5,
                event_param6 = line.EventParam6,
                action1_type = line.Action1Type,
                action1_param1 = line.Action1Param1,
                action1_param2 = line.Action1Param2,
                action1_param3 = line.Action1Param3,
                action2_type = line.Action2Type,
                action2_param1 = line.Action2Param1,
                action2_param2 = line.Action2Param2,
                action2_param3 = line.Action2Param3,
                action3_type = line.Action3Type,
                action3_param1 = line.Action3Param1,
                action3_param2 = line.Action3Param2,
                action3_param3 = line.Action3Param3,
                comment = line.Comment,
            };
        }

        private async Task BuildUpdate(IMultiQuery query)
        {
            uint? entry;
            if (script.EntryOrGuid >= 0)
                entry = (uint)script.EntryOrGuid;
            else
                entry = (await databaseProvider.GetCreatureByGuidAsync(0, (uint)-script.EntryOrGuid))?.Entry;

            if (entry.HasValue)
            {
                query
                    .Table(DatabaseTable.WorldTable("creature_template"))
                    .Where(t => t.Column<int>("Entry") == (int)entry.Value)
                    .Set("AIName", "EventAI")
                    .Set("ScriptName", "")
                    .Update();
            }
            else
                query.Comment("[WARNING] cannot set creature AI to EventAI, because guid not found in `creature` table!");
        }

        private void BuildDelete(IMultiQuery query, int entryOrGuid, IEventAiLine[] serializedLines)
        {
            query.Table(EventAiTableName).Where(r => r.Column<int>("creature_id") == entryOrGuid).Delete();
        }
    }
}