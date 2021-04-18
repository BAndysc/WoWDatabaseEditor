using System.Collections.ObjectModel;

namespace WDE.Common.Solution
{
    public class MetaSolutionSQL : ISolutionItem
    {
        public MetaSolutionSQL(ISolutionItem itemToGenerate)
        {
            ItemToGenerate = itemToGenerate;
        }
        
        public bool IsContainer => false;
        public ObservableCollection<ISolutionItem> Items => null;
        public string ExtraId => null;
        public bool IsExportable => false;
        public ISolutionItem ItemToGenerate { get; }
    }
}