namespace WDE.Common.Database
{
    public enum SmartScriptType
    {
        Creature = 0,
        GameObject = 1,
        AreaTrigger = 2,
        Event = 3,
        Gossip = 4,
        Quest = 5,
        Spell = 6,
        Transport = 7,
        Instance = 8,
        TimedActionList = 9,
        Scene = 10,
        AreaTriggerEntity = 11,
        AreaTriggerEntityServerSide = 12,
        // do not exists in TC
        Aura = 13,
        Cinematic = 14,
        PlayerChoice = 15,
        Template,
        StaticSpell,
        BattlePet,
        Conversation,
        END,
        
        ActionList,  // can be any id, do not delete
    }
    
    public enum AnimTier : byte
    {
        Ground = 0,
        Swim = 1,
        Hover = 2,
        Fly = 3,
        Submerged = 4
    }
}