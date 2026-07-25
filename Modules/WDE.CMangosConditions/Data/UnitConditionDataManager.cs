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
    public interface IUnitConditionDataProvider
    {
        Task<IReadOnlyList<UnitConditionVariableJson>> GetVariables();
    }

    [AutoRegister]
    [SingleInstance]
    public class UnitConditionDataProvider : IUnitConditionDataProvider
    {
        private const string VariablesPath = "MangosConditionsData/unit_conditions.json";

        private readonly IRuntimeDataService runtimeDataService;

        public UnitConditionDataProvider(IRuntimeDataService runtimeDataService)
        {
            this.runtimeDataService = runtimeDataService;
        }

        public async Task<IReadOnlyList<UnitConditionVariableJson>> GetVariables() =>
            JsonConvert.DeserializeObject<List<UnitConditionVariableJson>>(await runtimeDataService.ReadAllText(VariablesPath)) ?? new();
    }

    [UniqueProvider]
    public interface IUnitConditionDataManager
    {
        IReadOnlyList<UnitConditionVariableJson> AllVariables { get; }
        UnitConditionVariableJson? TryGetVariable(int id);
    }

    [AutoRegister]
    [SingleInstance]
    public class UnitConditionDataManager : IUnitConditionDataManager, IGlobalAsyncInitializer
    {
        private readonly IUnitConditionDataProvider provider;
        private Dictionary<int, UnitConditionVariableJson> byId = new();

        public UnitConditionDataManager(IUnitConditionDataProvider provider)
        {
            this.provider = provider;
        }

        public async Task Initialize()
        {
            AllVariables = (await provider.GetVariables()).ToList();
            byId = AllVariables.ToDictionary(v => v.Id, v => v);
        }

        public IReadOnlyList<UnitConditionVariableJson> AllVariables { get; private set; } = Array.Empty<UnitConditionVariableJson>();

        public UnitConditionVariableJson? TryGetVariable(int id) => byId.TryGetValue(id, out var data) ? data : null;
    }
}
