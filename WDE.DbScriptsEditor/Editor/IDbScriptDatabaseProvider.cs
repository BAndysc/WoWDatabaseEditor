using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.DbScriptsEditor.Models;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Editor
{
    [UniqueProvider]
    public interface IDbScriptDatabaseProvider
    {
        Task<IReadOnlyList<IDbScriptLine>> GetScript(DbScriptType type, uint id);
        Task<IReadOnlyList<uint>> GetScriptIds(DbScriptType type);
        Task<IReadOnlyList<IMangosConditionLine>> GetConditions(IReadOnlyList<uint> entries);
        Task<uint> GetMaxConditionEntry();
    }

    [AutoRegister]
    [SingleInstance]
    public class DbScriptDatabaseProvider : IDbScriptDatabaseProvider
    {
        private readonly IMangosDatabaseProvider databaseProvider;

        public DbScriptDatabaseProvider(IMangosDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }

        public Task<IReadOnlyList<IDbScriptLine>> GetScript(DbScriptType type, uint id) =>
            databaseProvider.GetDbScript(DbScriptTypes.TableName(type), id);

        public Task<IReadOnlyList<uint>> GetScriptIds(DbScriptType type) =>
            databaseProvider.GetDbScriptIds(DbScriptTypes.TableName(type));

        public Task<IReadOnlyList<IMangosConditionLine>> GetConditions(IReadOnlyList<uint> entries) =>
            databaseProvider.GetConditionsByEntries(entries);

        public Task<uint> GetMaxConditionEntry() =>
            databaseProvider.GetMaxConditionEntry();
    }
}
