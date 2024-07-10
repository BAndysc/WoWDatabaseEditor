using System.Collections.Generic;
using Prism.Events;
using WDE.Module.Attributes;

namespace WDE.Common.DBC
{
    [UniqueProvider]
    public interface IDbcStore
    {
        bool IsConfigured { get; }
        Dictionary<long, string> AreaTriggerStore { get; }
        Dictionary<long, string> PhaseStore { get; }
        Dictionary<long, string> MapStore { get; }
        Dictionary<long, string> SoundStore { get; }
        Dictionary<long, string> ClassStore { get; }
        Dictionary<long, string> RaceStore { get; }
        Dictionary<long, string> EmoteStore { get; }
        Dictionary<long, string> MapDirectoryStore { get; }
        Dictionary<long, string> SceneStore { get; }
        Dictionary<long, Dictionary<long, long>> ScenarioToStepStore { get; }
        Dictionary<long, string> ScenarioStepStore { get; }
        Dictionary<long, long> BattlePetSpeciesIdStore { get; }
    }
    
    public class DbcLoadedEvent : PubSubEvent<IDbcStore>
    {
    }
}
