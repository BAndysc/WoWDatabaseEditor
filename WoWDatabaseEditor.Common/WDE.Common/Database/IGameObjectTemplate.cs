namespace WDE.Common.Database
{
    public interface IGameObjectTemplate
    {
        uint Entry { get; }
        uint DisplayId { get; }
        float Size { get; }
        GameobjectType Type { get; }
        public uint FlagsExtra => 0;
        string Name { get; }
        string AIName { get; }
        string ScriptName { get; }
        uint this[int dataIndex] { get; }
        uint DataCount { get; }
    }
    
    public enum GameobjectType
    {
        Door                   = 0,
        Button                 = 1,
        QuestGiver             = 2,
        Chest                  = 3,
        Binder                 = 4,
        Generic                = 5,
        Trap                   = 6,
        Chair                  = 7,
        SpellFocus            = 8,
        Text                   = 9,
        Goober                 = 10,
        Transport              = 11,
        AreaDamage             = 12,
        Camera                 = 13,
        MapObject             = 14,
        MoTransport           = 15,
        DuelArbiter           = 16,
        FishingNode            = 17,
        SummoningRitual       = 18,
        Mailbox                = 19,
        DoNotUse             = 20,
        GuardPost              = 21,
        SpellCaster            = 22,
        MeetingStone           = 23,
        FlagStand              = 24,
        FishingHole            = 25,
        FlagDrop               = 26,
        MiniGame              = 27,
        DoNotUse2           = 28,
        CapturePoint          = 29,
        AuraGenerator         = 30,
        DungeonDifficulty     = 31,
        BarberChair           = 32,
        DestructibleBuilding  = 33,
        GuildBank             = 34,
        Trapdoor               = 35
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
    }
}