using System.Collections.ObjectModel;

namespace WDE.Common.Solution
{
    public class MetaSolutionSQL : ISolutionItem
    {
        public MetaSolutionSQL(params ISolutionItem[] itemsToGenerate)
        {
            ItemsToGenerate = itemsToGenerate;
        }
        
        public bool IsContainer => false;
        public ObservableCollection<ISolutionItem>? Items => null;
        public string? ExtraId => null;
        public bool IsExportable => false;
        public ISolutionItem[] ItemsToGenerate { get; }
    }
}