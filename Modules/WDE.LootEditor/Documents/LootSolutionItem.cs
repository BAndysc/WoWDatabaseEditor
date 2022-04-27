using System.Collections.ObjectModel;
using WDE.Common;

namespace WDE.LootEditor.Documents;

public class LootSolutionItem : ISolutionItem
{
    public LootSolutionItem(uint entry)
    {
        
    }
    
    public bool IsContainer => false;
    public ObservableCollection<ISolutionItem>? Items => null;
    public string? ExtraId => null;
    public bool IsExportable { get; set; }
    public ISolutionItem Clone()
    {
        return new LootSolutionItem(0);
    }
}