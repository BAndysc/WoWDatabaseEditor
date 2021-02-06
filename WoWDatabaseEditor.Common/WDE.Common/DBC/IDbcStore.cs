using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.DBC
{
    [UniqueProvider]
    public interface IDbcStore
    {
        Dictionary<int, string> AreaTriggerStore { get; }
        Dictionary<int, string> SkillStore { get; }
        Dictionary<int, string> LanguageStore { get; }
        Dictionary<int, string> PhaseStore { get; }
        Dictionary<int, string> AreaStore { get; }
        Dictionary<int, string> MapStore { get; }
        Dictionary<int, string> SoundStore { get; }
        Dictionary<int, string> MovieStore { get; }
        Dictionary<int, string> ClassStore { get; }
        Dictionary<int, string> RaceStore { get; }
        Dictionary<int, string> EmoteStore { get; }
        Dictionary<int, string> AchievementStore { get; }
        Dictionary<int, string> ItemStore { get; }
    }
}