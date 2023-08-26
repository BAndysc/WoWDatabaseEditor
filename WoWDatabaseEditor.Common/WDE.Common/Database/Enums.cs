using System;

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

    public static class SmartScriptTypeExtensions
    {
        public static SmartScriptTypeMask ToMask(this SmartScriptType type)
        {
            return (SmartScriptTypeMask)(1 << (int)type);
        }
    }
    
    [Flags]
    public enum SmartScriptTypeMask
    {
        Creature = 1 << SmartScriptType.Creature,
        GameObject = 1 << SmartScriptType.GameObject,
        AreaTrigger = 1 << SmartScriptType.AreaTrigger,
        Event = 1 << SmartScriptType.Event,
        Gossip = 1 << SmartScriptType.Gossip,
        Quest = 1 << SmartScriptType.Quest,
        Spell = 1 << SmartScriptType.Spell,
        Transport = 1 << SmartScriptType.Transport,
        Instance = 1 << SmartScriptType.Instance,
        TimedActionList = 1 << SmartScriptType.TimedActionList,
        Scene = 1 << SmartScriptType.Scene,
        AreaTriggerEntity = 1 << SmartScriptType.AreaTriggerEntity,
        AreaTriggerEntityServerSide = 1 << SmartScriptType.AreaTriggerEntityServerSide,
        Aura = 1 << SmartScriptType.Aura,
        Cinematic = 1 << SmartScriptType.Cinematic,
        PlayerChoice = 1 << SmartScriptType.PlayerChoice,
        Template = 1 << SmartScriptType.Template,
        StaticSpell = 1 << SmartScriptType.StaticSpell,
        BattlePet = 1 << SmartScriptType.BattlePet,
        Conversation = 1 << SmartScriptType.Conversation,
        ActionList = 1 << SmartScriptType.ActionList,  // can be any id, do not delete
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