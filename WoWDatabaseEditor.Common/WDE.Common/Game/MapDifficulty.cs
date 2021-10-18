namespace WDE.Common.Game
{
    public enum LegacyMapDifficulty
    {
        NormalDungeonOr10ManNormalRaid = 0,
        HeroicDungeonOr25ManNormalRaid = 1,
        HeroicRaid10Player = 2,
        HeroicRaid25Player = 3,
        LookingForRaid = 4
    }
    
    public enum MapDifficulty : uint
    {
        DungeonNormal = 1,
        DungeonHeroic = 2,
        Raid10Player = 3,
        Raid25Player = 4,
        Raid10PlayerHeroic = 5,
        Raid25PlayerHeroic = 6,
        RaidLookingForRaid = 7,
        DungeonMythicKeystone = 8,
        Raid40Player = 9,
        ScenarioHeroicScenario = 11,
        ScenarioNormalScenario = 12,
        RaidNormal = 14,
        RaidHeroic = 15,
        RaidMythic = 16,
        RaidLookingForRaidNew = 17,
        RaidEvent = 18,
        DungeonEvent = 19,
        ScenarioEventScenario = 20,
        DungeonMythic = 23,
        DungeonTimeWalking = 24,
        ScenarioWorldPvPScenario = 25,
        PvpBattlefieldPvEvPScenario = 29,
        ScenarioEvent = 30,
        ScenarioWorldPvPScenario2 = 32,
        RaidTimeWalking = 33,
        PvpBattlefieldPvP = 34,
        ScenarioNormal = 38,
        ScenarioHeroic = 39,
        ScenarioMythic = 40,
        ScenarioPvP = 45,
        ScenarioNormal2 = 147,
        ScenarioHeroic2 = 149,
        DungeonNormalNew = 150,
        RaidLookingForRaidNew2 = 151,
        ScenarioVisionsOfNZoth = 152,
        ScenarioTeemingIsland = 153,
        ScenarioTorghast = 167,
        ScenarioPathOfAscensionCourage = 168,
        ScenarioPathOfAscensionLoyalty = 169,
        ScenarioPathOfAscensionWisdom = 170,
        ScenarioPathOfAscensionHumility = 171,
        WorldBoss = 172,
    }
    
    public static class MapDifficultyExtensions
    {
        public static LegacyMapDifficulty? ToLegacyMapDifficulty(this MapDifficulty mapDifficulty)
        {
            switch (mapDifficulty)
            {
                case MapDifficulty.DungeonNormal:
                case MapDifficulty.Raid10Player:
                    return LegacyMapDifficulty.NormalDungeonOr10ManNormalRaid;
                case MapDifficulty.DungeonHeroic:
                case MapDifficulty.Raid25Player:
                    return LegacyMapDifficulty.HeroicDungeonOr25ManNormalRaid;
                case MapDifficulty.Raid10PlayerHeroic:
                    return LegacyMapDifficulty.HeroicRaid10Player;
                case MapDifficulty.Raid25PlayerHeroic:
                    return LegacyMapDifficulty.HeroicRaid25Player;
                case MapDifficulty.RaidLookingForRaid:
                    return LegacyMapDifficulty.LookingForRaid;
                default:
                    return null;
            }
        }
    }
}