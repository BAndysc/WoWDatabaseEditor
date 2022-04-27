using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;
using WDE.QuestChainEditor.QueryGenerators;

namespace WDE.QuestChainEditor.Documents;

public class QuestChainSolutionItem : ISolutionItem
{
    private List<uint> entries = new();
    private Dictionary<uint, ChainRawData> existingData = new();

    public IReadOnlyList<uint> Entries => entries;
    public IReadOnlyDictionary<uint, ChainRawData> ExistingData => existingData;

    [JsonIgnore] 
    public bool IsContainer => false;
    
    [JsonIgnore]
    public ObservableCollection<ISolutionItem>? Items { get; set; }

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

        return copy;
    }
}