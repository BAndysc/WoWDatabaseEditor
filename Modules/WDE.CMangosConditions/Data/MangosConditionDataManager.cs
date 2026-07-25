using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common.Modules;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.CMangosConditions.Data
{
    [UniqueProvider]
    public interface IMangosConditionDataProvider
    {
        Task<IReadOnlyList<MangosConditionJson>> GetConditions();
    }

    [AutoRegister]
    [SingleInstance]
    public class MangosConditionDataProvider : IMangosConditionDataProvider
    {
        private const string ConditionsPath = "MangosConditionsData/conditions.json";

        private readonly IRuntimeDataService runtimeDataService;

        public MangosConditionDataProvider(IRuntimeDataService runtimeDataService)
        {
            this.runtimeDataService = runtimeDataService;
        }

        public async Task<IReadOnlyList<MangosConditionJson>> GetConditions() =>
            JsonConvert.DeserializeObject<List<MangosConditionJson>>(await runtimeDataService.ReadAllText(ConditionsPath)) ?? new();
    }

    [UniqueProvider]
    public interface IMangosConditionDataManager
    {
        IReadOnlyList<MangosConditionJson> AllConditions { get; }
        MangosConditionJson? TryGetCondition(int id);
    }

    [AutoRegister]
    [SingleInstance]
    public class MangosConditionDataManager : IMangosConditionDataManager, IGlobalAsyncInitializer
    {
        private readonly IMangosConditionDataProvider provider;
        private Dictionary<int, MangosConditionJson> byId = new();

        public MangosConditionDataManager(IMangosConditionDataProvider provider)
        {
            this.provider = provider;
        }

        public async Task Initialize()
        {
            AllConditions = (await provider.GetConditions()).ToList();
            byId = AllConditions.ToDictionary(c => c.Id, c => c);
        }

        public IReadOnlyList<MangosConditionJson> AllConditions { get; private set; } = Array.Empty<MangosConditionJson>();

        public MangosConditionJson? TryGetCondition(int id) => byId.TryGetValue(id, out var data) ? data : null;
    }
}
