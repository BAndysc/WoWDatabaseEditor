using DynamicData.Binding;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    public class DatabaseEntitiesGroupViewModel : ObservableCollectionExtended<DatabaseEntityViewModel>
    {
        public uint Key { get; }
        public string Name { get; }

        public DatabaseEntitiesGroupViewModel(uint key, string name)
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