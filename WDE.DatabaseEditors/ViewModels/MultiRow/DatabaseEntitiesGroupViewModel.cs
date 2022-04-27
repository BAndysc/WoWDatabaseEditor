using DynamicData.Binding;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    public class DatabaseEntitiesGroupViewModel : ObservableCollectionExtended<DatabaseEntityViewModel>
    {
        public DatabaseKey Key { get; }
        public string Name { get; }

        public DatabaseEntitiesGroupViewModel(DatabaseKey key, string name)
        {
            Key = key;
            Name = name;
        }

        public DatabaseEntityViewModel? GetAndRemove(DatabaseEntity entity)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (this[i].Entity == entity)
                {
                    var vm = this[i];
                    RemoveAt(i);
                    return vm;
                }
            }

            return null;
        }
    }
}