using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Database;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Documents;

public class QuestChainSolutionItem : ISolutionItem
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
}