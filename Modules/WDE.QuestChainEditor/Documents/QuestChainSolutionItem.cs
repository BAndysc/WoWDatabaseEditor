using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Database;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Documents;

public class QuestChainSolutionItem : ISolutionItem, IEquatable<QuestChainSolutionItem>
{
    private List<uint> entries = new();
    private Dictionary<uint, ChainRawData> existingData = new();
    private Dictionary<uint, List<AbstractCondition>> existingConditions = new();

    public IReadOnlyList<uint> Entries => entries;
    public IReadOnlyDictionary<uint, ChainRawData> ExistingData => existingData;
    public IReadOnlyDictionary<uint, List<AbstractCondition>> ExistingConditions => existingConditions;

    [JsonIgnore] 
    public bool IsContainer => false;

    [JsonIgnore]
    public ObservableCollection<ISolutionItem>? Items => null;

    [JsonIgnore]
    public string? ExtraId => null;
    
    [JsonIgnore]
    public bool IsExportable => true;

    public ISolutionItem Clone()
    {
        var copy = new QuestChainSolutionItem();
        copy.entries.AddRange(entries);
        foreach (var existing in existingData)
        {
            copy.existingData[existing.Key] = existing.Value.Clone();
        }

        foreach (var (questId, conditions) in existingConditions)
        {
            copy.existingConditions[questId] = conditions.Select(c => new AbstractCondition(c)).ToList();
        }

        return copy;
    }

    public void AddEntry(uint entry)
    {
        entries.Add(entry);
    }

    // intended - there can be only one QuestChainSolutionItem in session
    public bool Equals(QuestChainSolutionItem? other) => true;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((QuestChainSolutionItem)obj);
    }

    public override int GetHashCode() => 0;

    public static bool operator ==(QuestChainSolutionItem? left, QuestChainSolutionItem? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(QuestChainSolutionItem? left, QuestChainSolutionItem? right)
    {
        return !Equals(left, right);
    }

    public void UpdateEntry(uint quest, ChainRawData existingOldData, IReadOnlyList<ICondition>? existingOldConditions)
    {
        if (!entries.Contains(quest))
            entries.Add(quest);
        existingData[quest] = existingOldData;
        if (existingOldConditions != null)
            existingConditions[quest] = existingOldConditions.Select(c => new AbstractCondition(c)).ToList();
        else
            existingConditions.Remove(quest);
    }
}