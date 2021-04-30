using System.Collections.ObjectModel;
using Prism.Mvvm;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.ViewModels.MultiRow
{
    // single database entity
    public class DatabaseEntityViewModel : BindableBase
    {
        public string Name { get; }
        public uint Key => Entity.Key;
        public DatabaseEntity Entity { get; }
        public ObservableCollection<DatabaseCellViewModel> Cells { get; } = new();

        public DatabaseEntityViewModel(DatabaseEntity entity, string name)
        {
            Name = name;
            Entity = entity;
        }
    }
}