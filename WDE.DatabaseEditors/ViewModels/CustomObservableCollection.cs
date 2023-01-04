using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using DynamicData.Binding;

namespace WDE.DatabaseEditors.ViewModels;

public class CustomObservableCollection<T> : ObservableCollectionExtended<T>
{
    protected bool pauseNotifications = false;

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (pauseNotifications)
            return;
        
        base.OnCollectionChanged(e);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (pauseNotifications)
            return;
        
        base.OnPropertyChanged(e);
    }

    /// <summary>
    /// fast ordering, without firing notifications
    /// </summary>
    /// <param name="indices"></param>
    public virtual void OrderByIndices(List<int> indices)
    {
        var sorted = indices.Select(i => this[i]).ToList();
        pauseNotifications = true;
        try
        {
            Clear();
            AddRange(sorted);
        }
        finally
        {
            pauseNotifications = false;
        }
    }
}