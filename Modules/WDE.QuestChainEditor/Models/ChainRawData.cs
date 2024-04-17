using System;
using System.Text.Json.Serialization;
using WDE.Common.Database;

namespace WDE.QuestChainEditor.Models;

public class ChainRawData
{
    [JsonConstructor]
    public ChainRawData(uint id, CharacterClasses allowableClasses, CharacterRaces allowableRaces, int prevQuestId = 0, int nextQuestId = 0, int exclusiveGroup = 0, int breadcrumbQuestId = 0)
    {
        Id = id;
        AllowableClasses = allowableClasses;
        AllowableRaces = allowableRaces;
        PrevQuestId = prevQuestId;
        NextQuestId = nextQuestId;
        ExclusiveGroup = exclusiveGroup;
        BreadcrumbQuestId = breadcrumbQuestId;
    }

    public ChainRawData(IQuestTemplate template)
    {
        Id = template.Entry;
        AllowableClasses = template.AllowableClasses;
        AllowableRaces = template.AllowableRaces;
        PrevQuestId = template.PrevQuestId;
        NextQuestId = template.NextQuestId;
        ExclusiveGroup = template.ExclusiveGroup;
        BreadcrumbQuestId = template.BreadcrumbForQuestId;
    }

    public uint Id { get; init; }
    public CharacterClasses AllowableClasses { get; }
    public CharacterRaces AllowableRaces { get; }
    public int PrevQuestId { get; set; }
    public int NextQuestId { get; set; }
    public int ExclusiveGroup { get; set; }
    public int BreadcrumbQuestId { get; set; }

    public override string ToString()
    {
        return $"({Id}, {PrevQuestId}, {NextQuestId}, {ExclusiveGroup}, {BreadcrumbQuestId}, {AllowableClasses}, {AllowableRaces})";
    }

    protected bool Equals(ChainRawData other)
    {
        return Id == other.Id && PrevQuestId == other.PrevQuestId && NextQuestId == other.NextQuestId && ExclusiveGroup == other.ExclusiveGroup && BreadcrumbQuestId == other.BreadcrumbQuestId
               && AllowableClasses == other.AllowableClasses && AllowableRaces == other.AllowableRaces;
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
        return new ChainRawData(Id, AllowableClasses, AllowableRaces, PrevQuestId, NextQuestId, ExclusiveGroup, BreadcrumbQuestId);
    }
}