using System.Collections.Generic;

namespace WDE.Common.Database
{
    public interface IGameObjectTemplate
    {
        uint Entry { get; }
        uint DisplayId { get; }
        float Size { get; }
        GameobjectType Type { get; }
        uint Flags { get; }
        public uint FlagsExtra => 0;
        string Name { get; }
        string AIName { get; }
        string ScriptName { get; }
        uint this[int dataIndex] { get; }
        uint DataCount { get; }
    }
    
    public class AbstractGameObjectTemplate : IGameObjectTemplate
    {
        public uint Entry { get; init; }
        public uint DisplayId { get; init; }
        public float Size { get; init; }
        public GameobjectType Type { get; init; }
        public uint Flags { get; init; }
        public string Name { get; init; } = "";
        public string AIName { get; init; } = "";
        public string ScriptName { get; init; } = "";
        public uint this[int dataIndex] => 0;
        public uint DataCount => 0;
    }
    
    public enum GameobjectType
    {
        Door = 0,
        Button = 1,
        QuestGiver = 2,
        Chest = 3,
        Binder = 4,
        Generic = 5,
        Trap = 6,
        Chair = 7,
        SpellFocus = 8,
        Text = 9,
        Goober = 10,
        Transport = 11,
        AreaDamage = 12,
        Camera = 13,
        MapObject = 14,
        MapObjTransport = 15,
        DuelArbiter = 16,
        FishingNode = 17,
        SummoningRitual = 18,
        Mailbox = 19,
        DoNotUse = 20,
        GuardPost = 21,
        SpellCaster = 22,
        MeetingStone = 23,
        FlagStand = 24,
        FishingHole = 25,
        FlagDrop = 26,
        MiniGame = 27,
        DoNotUse2 = 28,
        ControlZone = 29,
        AuraGenerator = 30,
        DungeonDifficulty = 31,
        BarberChair = 32,
        DestructibleBuilding = 33,
        GuildBank = 34,
        Trapdoor = 35,
        NewFlag = 36,
        NewFlagDrop = 37,
        GarrisonBuilding = 38,
        GarrisonPlot = 39,
        ClientCreature = 40,
        ClientItem = 41,
        CapturePoint = 42,
        PhaseableMo = 43,
        GarrisonMonument = 44,
        GarrisonShipment = 45,
        GarrisonMonumentPlaque = 46,
        ItemForge = 47,
        UiLink = 48,
        KeystoneReceptacle = 49,
        GatheringNode = 50,
        ChallengeModeReward = 51,
        Multi = 52,
        SiegeableMulti = 53,
        SiegeableMo = 54,
        PvpReward = 55,
        PlayerChoiceChest = 56,
        LegendaryForge = 57,
        GarrTalentTree = 58,
        WeeklyRewardChest = 59,
        ClientModel = 60,
        CraftingTable = 61,
        PerksProgramChest = 62,
    }

    public static class GameObjectTemplateExtensions
    {
        public static uint GetGossipMenuId(this IGameObjectTemplate template)
        {
            if (template.Type == GameobjectType.QuestGiver)
                return template[3];
            
            if (template.Type == GameobjectType.Goober)
                return template[19];

            return 0;
        }
        
        public static uint? GetLootId(this IGameObjectTemplate template)
        {
            if (template.Type is GameobjectType.Chest &&
                template[1] == 0)
            {
                return template[30];
            }
            
            if (template.Type is GameobjectType.Chest or
                GameobjectType.FishingHole or
                GameobjectType.GatheringNode or
                GameobjectType.ChallengeModeReward)
                return template[1];

            return null;
        }

        public static uint GetMainPlayerCondition(this IGameObjectTemplate template)
        {
            switch (template.Type)
            {
                case GameobjectType.Door:
                    return template[7];
                case GameobjectType.Button:
                    return template[9];
                case GameobjectType.QuestGiver:
                    return template[10];
                case GameobjectType.Chest:
                    return template[17];
                case GameobjectType.Generic:
                    return template[6];
                case GameobjectType.Trap:
                    return template[15];
                case GameobjectType.Chair:
                    return template[4];
                case GameobjectType.SpellFocus:
                    return template[8];
                case GameobjectType.Text:
                    return template[4];
                case GameobjectType.Goober:
                    return template[22];
                case GameobjectType.Camera:
                    return template[4];
                case GameobjectType.SummoningRitual:
                    return template[8];
                case GameobjectType.Mailbox:
                    return template[0];
                case GameobjectType.SpellCaster:
                    return template[5];
                case GameobjectType.FlagStand:
                    return template[8];
                case GameobjectType.AuraGenerator:
                    return template[3];
                case GameobjectType.NewFlag:
                    return template[4];
                case GameobjectType.GarrisonMonumentPlaque:
                    return template[0];
                case GameobjectType.GatheringNode:
                    return template[11];
                default:
                    return 0;
            }
        }
        
        public static uint GetSecondaryPlayerCondition(this IGameObjectTemplate template)
        {
            if (template.Type == GameobjectType.AuraGenerator)
                 return template[5];

            return 0;
        }
        
        public static IEnumerable<uint> GetAllPlayerConditions(this IGameObjectTemplate template)
        {
            yield return template.GetMainPlayerCondition();

            if (template.Type == GameobjectType.AuraGenerator)
                yield return template[5];
        }
    }
}