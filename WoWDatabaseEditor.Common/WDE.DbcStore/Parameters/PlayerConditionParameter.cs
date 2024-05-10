using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WDE.Common.Database;
using WDE.Common.DBC.Structs;
using WDE.Common.Parameters;

namespace WDE.DbcStore.Parameters;

public class PlayerConditionParameter : Parameter, IDynamicParameter<long>
{
    private readonly IParameter<long> powerTypeParameter;
    private readonly IParameter<long> chrSpecializationRoleParameter;
    private readonly IParameter<long> skillParameter;
    private readonly IParameter<long> languages;
    private readonly IParameter<long> factions;
    private readonly IParameter<long> reputationRank;
    private readonly IParameter<long> quests;
    private readonly IParameter<long> spells;
    private readonly IParameter<long> items;
    private readonly IParameter<long> currencies;
    private readonly IParameter<long> zoneAreas;
    private readonly IParameter<long> achievements;
    private readonly IParameter<long> phases;
    private readonly IParameter<long> creatures;
    private readonly IReadOnlyList<IPlayerCondition> playerConditions;

    public PlayerConditionParameter(IParameter<long> powerTypeParameter,
        IParameter<long> chrSpecializationRoleParameter,
        IParameter<long> skillParameter,
        IParameter<long> languages,
        IParameter<long> factions,
        IParameter<long> reputationRank,
        IParameter<long> quests,
        IParameter<long> spells,
        IParameter<long> items,
        IParameter<long> currencies,
        IParameter<long> zoneAreas,
        IParameter<long> achievements,
        IParameter<long> phases,
        IParameter<long> creatures,
        IReadOnlyList<IPlayerCondition> playerConditions)
    {
        this.powerTypeParameter = powerTypeParameter;
        this.chrSpecializationRoleParameter = chrSpecializationRoleParameter;
        this.skillParameter = skillParameter;
        this.languages = languages;
        this.factions = factions;
        this.reputationRank = reputationRank;
        this.quests = quests;
        this.spells = spells;
        this.items = items;
        this.currencies = currencies;
        this.zoneAreas = zoneAreas;
        this.achievements = achievements;
        this.phases = phases;
        this.creatures = creatures;
        this.playerConditions = playerConditions;
        Items = new Dictionary<long, SelectOption>();
        Reload();

        var parameters = new[] {
            powerTypeParameter, chrSpecializationRoleParameter, skillParameter, languages, factions, reputationRank,
            quests, spells, items, currencies, zoneAreas, achievements, phases, creatures
        };

        foreach (var p in parameters.OfType<IDynamicParameter<long>>())
        {
            p.ItemsChanged += x => Reload();
        }
    }

    private void Reload()
    {
        Items!.Clear();
        foreach (var c in playerConditions)
            Items.Add(c.Id, new SelectOption(GetDescription(c), c.Failure_description_lang));
        ItemsChanged?.Invoke(this);
    }

    private string GetDescription(IPlayerCondition condition)
    {
        List<string> requirements = new();

        string ComparisonOp(int comparison)
        {
            return comparison switch
            {
                1 => "==",
                2 => "!=",
                3 => ">",
                4 => ">=",
                5 => "<",
                6 => "<=",
                _ => "unknown"
            };
        }

        string? MinMax(int min, int max, string description)
        {
            if (min > 0 && max > 0)
                return $"{description} >= {min} && {description} <= {max}";
            else if (min > 0)
                return $"{description} >= {min}";
            else if (max > 0)
                return $"{description} <= {max}";
            return null;
        }

        void AddMinMax(int min, int max, string description)
        {
            if (MinMax(min, max, description) is { } x)
                requirements.Add(x);
        }

        void Gender(int gender, string description)
        {
            if (gender == 3 || gender == -1)
                return; // any

            if (gender == 0)
                requirements.Add($"{description}: male");
            else if (gender == 1)
                requirements.Add($"{description}: female");
            else if (gender == 2)
                requirements.Add($"{description}: none");
            else
                requirements.Add($"{description}: {gender}");
        }

        void PowerType()
        {
            if (condition.PowerType != -1 && condition.PowerTypeComp != 0)
            {
                string requiredPowerValue = (condition.Flags & 4) == 4 ? "max" : condition.PowerTypeValue.ToString();
                var power = powerTypeParameter.ToString(condition.PowerType, ToStringOptions.WithoutNumber);
                requirements.Add($"current {power} {ComparisonOp(condition.PowerTypeComp)} {requiredPowerValue}");
            }
        }

        void SpecIndex()
        {
            if (condition.ChrSpecializationIndex >= 0)
                requirements.Add($"current spec index is {condition.ChrSpecializationIndex}");
        }

        void SpecRole()
        {
            if (condition.ChrSpecializationRole >= 0)
            {
                var role = chrSpecializationRoleParameter.ToString(condition.ChrSpecializationRole, ToStringOptions.WithoutNumber);
                requirements.Add($"current spec role is {role}");
            }
        }

        void Skills()
        {
            AddLogic(condition.SkillLogic, Enumerable.Range(0, condition.SkillIDCount)
                .Select(i => (skillId: condition.GetSkillID(i), name: skillParameter.ToString(condition.GetSkillID(i)),
                    min: condition.GetMinSkill(i), max: condition.GetMaxSkill(i)))
                .Select(tuple => tuple.skillId != 0 ? MinMax(tuple.min, tuple.max, $"skill {tuple.name}") : null)
                .ToList());
        }

        void Language()
        {
            if (condition.LanguageID != 0)
            {
                var language = languages.ToString(condition.LanguageID, ToStringOptions.WithoutNumber);
                AddMinMax(condition.MinLanguage, condition.MaxLanguage, $"language {language}");
            }
        }

        void FactionId()
        {
            List<string?> conds = new();
            for (int i = 0; i < condition.MinFactionIDCount; ++i)
            {
                if (condition.GetMinFactionID(i) > 0)
                {
                    var faction = factions.ToString(condition.GetMinFactionID(i), ToStringOptions.WithoutNumber);
                    var rank = reputationRank.ToString(condition.GetMinReputation(i), ToStringOptions.WithoutNumber);
                    conds.Add($"reputation to {faction} >= {rank}");
                }
            }

            if (condition.MaxFactionID != 0)
            {
                var faction = factions.ToString(condition.MaxFactionID, ToStringOptions.WithoutNumber);
                var rank = reputationRank.ToString(condition.MaxReputation, ToStringOptions.WithoutNumber);
                conds.Add($"reputation to {faction} <= {rank}");
            }

            AddLogic(condition.ReputationLogic, conds);
        }

        void PvPMedal()
        {
            if (condition.PvpMedal != 0)
            {
                requirements.Add($"pvp medal: {condition.PvpMedal}");
            }
        }

        void LifetimeMaxPvPRank()
        {
            if (condition.LifetimeMaxPVPRank != 0)
                requirements.Add($"lifetime max pvp rank: {condition.LifetimeMaxPVPRank}");
        }

        void PartyStatus()
        {
            if (condition.PartyStatus == 1)
                requirements.Add("not in party");
            else if (condition.PartyStatus == 2)
                requirements.Add("in a party");
            else if (condition.PartyStatus == 3)
                requirements.Add("in a non-raid party");
            else if (condition.PartyStatus == 4)
                requirements.Add("in a raid party");
            else if (condition.PartyStatus == 5)
                requirements.Add("not in party OR in a non-raid party");
        }

        void RewardedQuest()
        {
            AddLogic(condition.PrevQuestLogic, Enumerable.Range(0, condition.PrevQuestIDCount)
                .Select(condition.GetPrevQuestID)
                .Select(quest => quest != 0 ? $"rewarded " + quests.ToString(quest) : null)
                .ToList());
        }

        void ActiveQuest()
        {
            AddLogic(condition.CurrQuestLogic, Enumerable.Range(0, condition.CurrQuestIDCount)
                .Select(condition.GetCurrQuestID)
                .Select(quest => quest != 0 ? $"active " + quests.ToString(quest) : null)
                .ToList());
        }

        void CompleteQuest()
        {
            AddLogic(condition.CurrentCompletedQuestLogic, Enumerable.Range(0, condition.CurrentCompletedQuestIDCount)
                .Select(condition.GetCurrentCompletedQuestID)
                .Select(quest => quest != 0 ? $"completed (not rewarded) " + quests.ToString(quest) : null)
                .ToList());
        }

        void LearnedSpells()
        {
            AddLogic(condition.SpellLogic, Enumerable.Range(0, condition.SpellIDCount)
                .Select(condition.GetSpellID)
                .Select(spell => spell != 0 ? $"learned " + spells.ToString(spell) : null)
                .ToList());
        }

        void HasAura()
        {
            AddLogic(condition.AuraSpellLogic, Enumerable.Range(0, condition.AuraSpellIDCount)
                .Select(condition.GetAuraSpellID)
                .Select(spell => spell != 0 ? $"has aura " + spells.ToString(spell) : null)
                .ToList());
        }

        void Achievements()
        {
            var accountWide = (condition.Flags & 2) == 2 ? " (account-wide)" : "";
            AddLogic(condition.AchievementLogic, Enumerable.Range(0, condition.AchievementCount)
                .Select(condition.GetAchievement)
                .Select(achi => achi != 0 ? $"completed achievement " + achievements.ToString(achi) + accountWide : null)
                .ToList());
        }

        void Time()
        {
            AddLogic(0b0101010101010101, Enumerable.Range(0, condition.TimeCount)
                .Select(condition.GetTime)
                .Select(time => time != 0 ? $"time {time}" : null)
                .ToList());
        }

        void WorldStateExpression()
        {
            if (condition.WorldStateExpressionID != 0)
                requirements.Add($"world state expression {condition.WorldStateExpressionID}");
        }

        void Weather()
        {
            if (condition.WeatherID != 0)
                requirements.Add($"weather {condition.WeatherID}");
        }

        void InArea()
        {
            AddLogic(condition.AreaLogic, Enumerable.Range(0, condition.AreaIDCount)
                .Select(condition.GetAreaID)
                .Select(area => area != 0 ? $"in area {zoneAreas.ToString(area)}" : null)
                .ToList());
        }

        void Phases()
        {
            if (condition.PhaseID != 0)
                requirements.Add($"in phase {phases.ToString(condition.PhaseID)}");

            if (condition.PhaseGroupID != 0)
                requirements.Add($"in phase group {condition.PhaseGroupID}");
        }

        void QuestKills()
        {
            AddLogic(condition.QuestKillLogic, Enumerable.Range(0, condition.QuestKillMonsterCount)
                .Select(condition.GetQuestKillMonster)
                .Select(monster => monster != 0 ? $"has objective {creatures.ToString(monster)} in quest {quests.ToString(condition.QuestKillID)} completed" : null)
                .ToList());
        }

        void MovementFlags()
        {
            AddLogic(0b0101010101010101, Enumerable.Range(0, condition.MovementFlagsCount)
                .Select(condition.GetMovementFlags)
                .Select(flags => flags != 0 ? $"has movement flag {flags}": null)
                .ToList());
        }

        void ModifierTreeId()
        {
            if (condition.ModifierTreeID != 0)
                requirements.Add($"has modifier tree {condition.ModifierTreeID}");
        }

        void WeaponSubclassMask()
        {
            if (condition.WeaponSubclassMask != 0)
                requirements.Add($"weapon subclass mask {condition.WeaponSubclassMask}");
        }

        void Lfg()
        {
            AddLogic(condition.LfgLogic, Enumerable.Range(0, condition.LfgCompareCount)
                .Select(i => (status: condition.GetLfgStatus(i), compare: condition.GetLfgCompare(i), value: condition.GetLfgValue(i)))
                .Select(tuple => tuple.compare != 0 ? $"lfg status {tuple.status} {ComparisonOp(tuple.compare)} {tuple.value}" : null)
                .ToList());
        }

        void HasItems()
        {
            var includingBank = condition.ItemFlags != 0 ? "" : " (only backpack)";
            AddLogic(condition.ItemLogic, Enumerable.Range(0, condition.ItemIDCount)
                .Select(condition.GetItemID)
                .Select((item, index) => item != 0 ? $"has item " + items.ToString(item) + (condition.GetItemCount(index) <= 1 ? "" : " count >= " + condition.GetItemCount(index)) + includingBank: null)
                .ToList());
        }

        void Currency()
        {
            AddLogic(condition.CurrencyLogic, Enumerable.Range(0, condition.CurrencyCountCount)
                .Select(condition.GetCurrencyID)
                .Select((currency, index) => currency != 0 ? $"has " + currencies.ToString(currency) + " count >= " + condition.GetCurrencyCount(index): null)
                .ToList());
        }

        void Explored()
        {
            AddLogic(0b0101010101010101, Enumerable.Range(0, condition.ExploredCount)
                .Select(condition.GetExplored)
                .Select(area => area != 0 ? "explored " + zoneAreas.ToString(area) : null)
                .ToList());
        }

        string? Logic(uint logic, List<string?> conditions)
        {
            if (conditions.Count == 0 || conditions.All(x => x == null))
                return null;

            string Negate(int index, string cond)
            {
                if (((logic >> (16 + index)) & 1) != 0)
                    return $"!({cond})";
                return cond;
            }

            string output = Negate(0, conditions[0] ?? "(?)");
            uint? outputOperator = null;

            for (int i = 1; i < conditions.Count; ++i)
            {
                if (conditions[i] == null)
                    continue;

                var op = (logic >> (2 * (i - 1))) & 3;

                if (outputOperator.HasValue && outputOperator != op)
                    output = $"({output})";
                if (op == 1)
                {
                    output = $"{output} AND {Negate(i, conditions[i]!)}";
                }
                else if (op == 2)
                {
                    output = $"{output} OR {Negate(i, conditions[i]!)}";
                }

                outputOperator = op;
            }

            return output;
        }

        void AddLogic(uint logic, List<string?> conditions)
        {
            if (Logic(logic, conditions) is { } l)
                requirements.Add(l);
        }

        AddMinMax(condition.MinLevel, condition.MaxLevel, "level");

        if (condition.RaceMask != 0)
            requirements.Add($"race: {(CharacterRaces)condition.RaceMask}");

        if (condition.ClassMask != 0)
            requirements.Add($"class: {(CharacterClasses)condition.ClassMask}");

        Gender(condition.Gender, "gender");
        Gender(condition.NativeGender, "native gender");
        PowerType();
        SpecIndex();
        SpecRole();
        Skills();
        Language();
        FactionId();
        PvPMedal();
        LifetimeMaxPvPRank();
        PartyStatus();
        RewardedQuest();
        ActiveQuest();
        CompleteQuest();
        LearnedSpells();
        HasItems();
        Currency();
        Explored();
        HasAura();
        Achievements();
        Time();
        WorldStateExpression();
        Weather();
        InArea();
        AddMinMax(condition.MinExpansionLevel, condition.MaxExpansionLevel, "expansion");
        AddMinMax(condition.MinExpansionTier, condition.MaxExpansionTier, "expansion tier");
        Phases();
        AddMinMax(condition.MinAvgItemLevel, condition.MaxAvgItemLevel, "avg item lvl");
        AddMinMax(condition.MinAvgEquippedItemLevel, condition.MaxAvgEquippedItemLevel, "avg equipped item lvl");
        AddMinMax(condition.MinGuildLevel, condition.MaxGuildLevel, "guild level");
        QuestKills();
        MovementFlags();
        ModifierTreeId();
        WeaponSubclassMask();
        Lfg();
        AddMinMax(condition.MinPVPRank, condition.MaxPVPRank, "pvp rank");

        return string.Join(", ", requirements);
    }

    public event Action<IParameter<long>>? ItemsChanged;
}