using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Managers;
using WDE.DbcStore.Data;
using WDE.DbcStore.Providers;
using WDE.Module.Attributes;

namespace WDE.DbcStore.ViewModels
{
    [AutoRegister]
    public class DBCConfigViewModel : BindableBase, IConfigurable
    {
        private DBCVersions dbcVersion;
        private string path;
        private bool skipLoading;
        private DBCLocale dbcLocale;

        public DBCConfigViewModel(IDbcSettingsProvider dbcSettings, IWindowManager windowManager)
        {
            path = dbcSettings.GetSettings().Path;
            skipLoading = dbcSettings.GetSettings().SkipLoading;
            dbcVersion = dbcSettings.GetSettings().DBCVersion;
            dbcLocale = dbcSettings.GetSettings().DBCLocale;
            
            PickFolder = new DelegateCommand(async () =>
            {
                var selectedPath = await windowManager.ShowFolderPickerDialog(Path);
                if (selectedPath != null)
                    Path = selectedPath;
            });
            Save = new DelegateCommand(() =>
            {
                dbcSettings.UpdateSettings(new DBCSettings {Path = Path, SkipLoading = SkipLoading, DBCVersion = DBCVersion});
                IsModified = false;
            });

            DBCVersions = new ObservableCollection<DBCVersions>(Enum.GetValues<DBCVersions>());
        }

        public string Path
        {
            get => path;
            set
            {
                SetProperty(ref path, value);
                IsModified = true;
            }
        }

        public bool SkipLoading
        {
            get => skipLoading;
            set
            {
                SetProperty(ref skipLoading, value);
                IsModified = true;
            }
        }

        public DBCVersions DBCVersion
        {
            get => dbcVersion;
            set
            {
                SetProperty(ref dbcVersion, value);
                IsModified = true;
            }
        }

        public DBCLocale DBCLocale
        {
            get => dbcLocale;
            set
            {
                SetProperty(ref dbcLocale, value);
                IsModified = true;
            }
        }

        private bool isModified;
        public string ShortDescription => "To get all editor features, you need to use DBC (client database) files. This should be the path to extracted DBC files, the same the server uses.";
        
        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }
        
        public ObservableCollection<DBCVersions> DBCVersions { get; }
        
        public string Name => "DBC";

        public ICommand Save { get; }
        public ICommand PickFolder { get; }

        public bool IsRestartRequired => true;
        public ConfigurableGroup Group => ConfigurableGroup.Basic;
    }
}