using System.ComponentModel;
using WDE.Updater.Data;

namespace WDE.Updater.Services
{
    public interface IUpdaterSettingsProvider : INotifyPropertyChanged
    {
        UpdaterSettings Settings { get; set; }
    }
}