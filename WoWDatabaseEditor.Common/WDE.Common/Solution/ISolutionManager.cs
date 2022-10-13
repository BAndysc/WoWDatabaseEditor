using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [UniqueProvider]
    public interface ISolutionManager
    {
        IReadOnlyObservableList<ISolutionItem> Items { get; }
        void Initialize();
        void Refresh(ISolutionItem item);
        void RefreshAll();

        event System.Action<ISolutionItem?> RefreshRequest;
        
        void Add(ISolutionItem item);
        void Insert(int index, ISolutionItem item);
        void Remove(ISolutionItem item);
        void RemoveAt(int index);
    }

    public interface IReadOnlyObservableList<out T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        
    }
    
    public class ObservableList<T> : ObservableCollection<T>, IReadOnlyObservableList<T> { }
}