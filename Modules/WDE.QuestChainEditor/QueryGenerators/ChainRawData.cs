using System;
using WDE.Common.Database;

namespace WDE.QuestChainEditor.QueryGenerators;

public class ChainRawData
{
    public ChainRawData(uint id, int prevQuestId = 0, int nextQuestId = 0, int exclusiveGroup = 0, uint nextQuestInChain = 0, int breadcrumbQuestId = 0)
    {
        Id = id;
        PrevQuestId = prevQuestId;
        NextQuestId = nextQuestId;
        ExclusiveGroup = exclusiveGroup;
        NextQuestInChain = nextQuestInChain;
        BreadcrumbQuestId = breadcrumbQuestId;
    }

    public ChainRawData(IQuestTemplate template)
    {
        Id = template.Entry;
        PrevQuestId = template.PrevQuestId;
        NextQuestId = template.NextQuestId;
        ExclusiveGroup = template.ExclusiveGroup;
        NextQuestInChain = template.NextQuestInChain;
        BreadcrumbQuestId = template.BreadcrumbForQuestId;
    }

    public uint Id { get; init; }
    public int PrevQuestId { get; set; }
    public int NextQuestId { get; set; }
    public int ExclusiveGroup { get; set; }
    public uint NextQuestInChain { get; set; }
    public int BreadcrumbQuestId { get; set; }

    public override string ToString()
    {
        return $"({Id}, {PrevQuestId}, {NextQuestId}, {ExclusiveGroup}, {NextQuestInChain}, {BreadcrumbQuestId})";
    }

    protected bool Equals(ChainRawData other)
    {
        return Id == other.Id && PrevQuestId == other.PrevQuestId && NextQuestId == other.NextQuestId && ExclusiveGroup == other.ExclusiveGroup && NextQuestInChain == other.NextQuestInChain && BreadcrumbQuestId == other.BreadcrumbQuestId;
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
        return new ChainRawData(Id, PrevQuestId, NextQuestId, ExclusiveGroup, NextQuestInChain, BreadcrumbQuestId);
    }
}