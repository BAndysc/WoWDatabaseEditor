using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace WDE.Common
{
    public interface ISolutionItem
    {
        bool IsContainer { get; }
        ObservableCollection<ISolutionItem>? Items { get; }

        [JsonIgnore]
        string? ExtraId { get; }

        [JsonIgnore]
        bool IsExportable { get; }

        ISolutionItem Clone();
        ISolutionItem Clone(System.Guid guid) => Clone();
    }

    public interface IRenameableSolutionItem : ISolutionItem
    {
        void Rename(string newName);
    }
}