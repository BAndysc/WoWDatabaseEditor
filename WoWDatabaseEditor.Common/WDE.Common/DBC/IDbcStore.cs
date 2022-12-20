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
        Dictionary<long, string> SkillStore { get; }
        Dictionary<long, string> LanguageStore { get; }
        Dictionary<long, string> PhaseStore { get; }
        Dictionary<long, string> AreaStore { get; }
        Dictionary<long, string> MapStore { get; }
        Dictionary<long, string> SoundStore { get; }
        Dictionary<long, string> MovieStore { get; }
        Dictionary<long, string> ClassStore { get; }
        Dictionary<long, string> RaceStore { get; }
        Dictionary<long, string> EmoteStore { get; }
        Dictionary<long, string> EmoteOneShotStore { get; }
        Dictionary<long, string> EmoteStateStore { get; } 
        Dictionary<long, string> AchievementStore { get; }
        Dictionary<long, string> ItemStore { get; }
        Dictionary<long, string> SpellFocusObjectStore { get; }
        Dictionary<long, string> QuestInfoStore { get; }
        Dictionary<long, string> CharTitleStore { get; }
        Dictionary<long, string> MapDirectoryStore { get; }
        Dictionary<long, string> SceneStore { get; }
        Dictionary<long, string> ScenarioStore { get; }
        Dictionary<long, Dictionary<long, long>> ScenarioToStepStore { get; }
        Dictionary<long, string> ScenarioStepStore { get; }
        Dictionary<long, long> BattlePetSpeciesIdStore { get; }
    }
    
    public class DbcLoadedEvent : PubSubEvent<IDbcStore>
    {
    }
}