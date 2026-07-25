using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Data
{
    [UniqueProvider]
    public interface IDbScriptDataProvider
    {
        Task<IReadOnlyList<DbScriptCommandJson>> GetCommands();
        Task<IReadOnlyList<DbScriptGroupJson>> GetGroups();
    }

    [AutoRegister]
    [SingleInstance]
    public class DbScriptDataProvider : IDbScriptDataProvider
    {
        private const string CommandsPath = "DbScriptData/commands.json";
        private const string GroupsPath = "DbScriptData/commands_groups.json";

        private readonly IRuntimeDataService runtimeDataService;

        public DbScriptDataProvider(IRuntimeDataService runtimeDataService)
        {
            this.runtimeDataService = runtimeDataService;
        }

        public async Task<IReadOnlyList<DbScriptCommandJson>> GetCommands() =>
            JsonConvert.DeserializeObject<List<DbScriptCommandJson>>(await runtimeDataService.ReadAllText(CommandsPath)) ?? new();

        public async Task<IReadOnlyList<DbScriptGroupJson>> GetGroups() =>
            JsonConvert.DeserializeObject<List<DbScriptGroupJson>>(await runtimeDataService.ReadAllText(GroupsPath)) ?? new();
    }
}
