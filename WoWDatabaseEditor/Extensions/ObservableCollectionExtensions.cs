using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WoWDatabaseEditorCore.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> list)
        {
            foreach (T l in list)
            {
                collection.Add(l);
            }
        }
    }
}