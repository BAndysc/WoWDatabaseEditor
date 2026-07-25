using System;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.Common.Solution;
using WDE.DbScriptsEditor.Data;
using WDE.DbScriptsEditor.Editor;
using WDE.DbScriptsEditor.Exporter;
using WDE.DbScriptsEditor.Models;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.DbScriptsEditor.Providers
{
    // Produces the SQL for a dbscript solution item without opening the editor (session/apply flow).
    // Loads the current DB rows and roundtrips them through the exporter.
    [AutoRegister]
    [SingleInstance]
    public class DbScriptSqlGenerator : ISolutionItemSqlProvider<DbScriptSolutionItem>
    {
        private readonly Lazy<IDbScriptDatabaseProvider> database;
        private readonly Lazy<IDbScriptDataManager> dataManager;
        private readonly Lazy<IParameterFactory> parameterFactory;
        private readonly Lazy<IDbScriptExporter> exporter;

        public DbScriptSqlGenerator(Lazy<IDbScriptDatabaseProvider> database,
            Lazy<IDbScriptDataManager> dataManager,
            Lazy<IParameterFactory> parameterFactory,
            Lazy<IDbScriptExporter> exporter)
        {
            this.database = database;
            this.dataManager = dataManager;
            this.parameterFactory = parameterFactory;
            this.exporter = exporter;
        }

        public async Task<IQuery> GenerateSql(DbScriptSolutionItem item)
        {
            var rows = await database.Value.GetScript(item.ScriptType, item.ScriptId);
            var script = new EditableDbScript(item.ScriptType, item.ScriptId, dataManager.Value, parameterFactory.Value);
            script.Load(rows);
            return exporter.Value.GenerateSql(script);
        }
    }
}
