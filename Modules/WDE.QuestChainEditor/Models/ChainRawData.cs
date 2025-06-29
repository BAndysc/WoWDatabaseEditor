using System;
using Newtonsoft.Json;
using WDE.Common.Database;

namespace WDE.QuestChainEditor.Models;

public record struct OtherFactionQuest(uint Id, OtherFactionQuest.Hint FactionHint)
{
    public enum Hint
    {
        None,
        Horde,
        Alliance
    }
}

public class ChainRawData
{
    [JsonConstructor]
    public ChainRawData(uint id, long allowableClasses, long allowableRaces, int prevQuestId = 0, int nextQuestId = 0, int exclusiveGroup = 0, int breadcrumbQuestId = 0, OtherFactionQuest? otherFactionQuest = null)
    {
        Id = id;
        AllowableClasses = allowableClasses;
        AllowableRaces = allowableRaces;
        PrevQuestId = prevQuestId;
        NextQuestId = nextQuestId;
        ExclusiveGroup = exclusiveGroup;
        BreadcrumbQuestId = breadcrumbQuestId;
        OtherFactionQuest = otherFactionQuest;
    }

    public ChainRawData(IQuestTemplate template, OtherFactionQuest? otherFactionQuest)
    {
        Id = template.Entry;
        AllowableClasses = (long)template.AllowableClasses;
        AllowableRaces = (long)template.AllowableRaces;
        PrevQuestId = template.PrevQuestId;
        NextQuestId = template.NextQuestId;
        ExclusiveGroup = template.ExclusiveGroup;
        BreadcrumbQuestId = template.BreadcrumbForQuestId;
        OtherFactionQuest = otherFactionQuest;
    }

    public uint Id { get; init; }
    public long AllowableClasses { get; }
    public long AllowableRaces { get; }
    public int PrevQuestId { get; set; }
    public int NextQuestId { get; set; }
    public int ExclusiveGroup { get; set; }
    public int BreadcrumbQuestId { get; set; }
    public OtherFactionQuest? OtherFactionQuest { get; set; }

    public override string ToString()
    {
        return $"({Id}, {PrevQuestId}, {NextQuestId}, {ExclusiveGroup}, {BreadcrumbQuestId}, {AllowableClasses}, {AllowableRaces})";
    }

    protected bool Equals(ChainRawData other)
    {
        return Id == other.Id && PrevQuestId == other.PrevQuestId && NextQuestId == other.NextQuestId && ExclusiveGroup == other.ExclusiveGroup && BreadcrumbQuestId == other.BreadcrumbQuestId
               && AllowableClasses == other.AllowableClasses && AllowableRaces == other.AllowableRaces
               && (OtherFactionQuest == null && other.OtherFactionQuest == null ||
                   OtherFactionQuest != null && other.OtherFactionQuest != null &&
                   OtherFactionQuest.Value.Id == other.OtherFactionQuest.Value.Id);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ChainRawData)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }

    public static bool operator ==(ChainRawData? left, ChainRawData? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ChainRawData? left, ChainRawData? right)
    {
        return !Equals(left, right);
    }

    public ChainRawData Clone()
    {
        return new ChainRawData(Id, AllowableClasses, AllowableRaces, PrevQuestId, NextQuestId, ExclusiveGroup, BreadcrumbQuestId, OtherFactionQuest);
    }
}