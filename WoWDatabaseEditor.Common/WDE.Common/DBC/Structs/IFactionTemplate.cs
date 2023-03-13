using System;

namespace WDE.Common.DBC.Structs;

public struct Faction
{
    public Faction(ushort factionId, string name)
    {
        FactionId = factionId;
        Name = name;
    }

    public ushort FactionId { get; }
    public string Name { get; }
}

public struct FactionTemplate
{
    public uint TemplateId { get; init; }
    public ushort Faction { get; init; }
    public ushort Flags { get; init; }
    public FactionGroupMask FactionGroup { get; init; }
    public FactionGroupMask FriendGroup { get; init; }
    public FactionGroupMask EnemyGroup { get; init; }
    public ushort[] Friends { get; init; }
    public ushort[] Enemies { get; init; }

    public static readonly FactionTemplate Horde = new FactionTemplate()
    {
        TemplateId = uint.MaxValue,
        FactionGroup = FactionGroupMask.Player | FactionGroupMask.Horde,
        FriendGroup = FactionGroupMask.Horde,
        EnemyGroup = FactionGroupMask.Alliance | FactionGroupMask.Monster
    };

    public static readonly FactionTemplate Alliance = new FactionTemplate()
    {
        TemplateId = uint.MaxValue - 1,
        FactionGroup = FactionGroupMask.Player | FactionGroupMask.Alliance,
        FriendGroup = FactionGroupMask.Alliance,
        EnemyGroup = FactionGroupMask.Horde | FactionGroupMask.Monster
    };
}

[Flags]
public enum FactionGroupMask
{
    Player = 1,
    Alliance = 2,
    Horde = 4,
    Monster = 8,
}

public static class FactionTemplateExtensions
{
    public static bool IsFriendlyTo(this FactionTemplate that, in FactionTemplate entry)
    {
        if (that.TemplateId == entry.TemplateId)
            return true;
        
        if (entry.Faction > 0)
        {
            for (int i = 0; i < that.Enemies.Length; ++i)
                if (that.Enemies[i] == entry.Faction)
                    return false;
            
            for (int i = 0; i < that.Friends.Length; ++i)
                if (that.Friends[i] == entry.Faction)
                    return true;
        }
        return ((that.FriendGroup & entry.FactionGroup) > 0) || ((that.FactionGroup & entry.FriendGroup) > 0);
    }
    
    public static bool IsHostileTo(this FactionTemplate that, in FactionTemplate entry)
    {
        if (that.TemplateId == entry.TemplateId)
            return false;
        
        if (entry.Faction > 0)
        {
            for (int i = 0; i < that.Enemies.Length; ++i)
                if (that.Enemies[i] == entry.Faction)
                    return true;
            
            for (int i = 0; i < that.Friends.Length; ++i)
                if (that.Friends[i] == entry.Faction)
                    return false;
        }
        return (that.EnemyGroup & entry.FactionGroup) != 0;
    }
}