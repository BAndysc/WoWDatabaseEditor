using System;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
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

        public DBCConfigViewModel(IDbcSettingsProvider dbcSettings)
        {
            path = dbcSettings.GetSettings().Path;
            skipLoading = dbcSettings.GetSettings().SkipLoading;
            dbcVersion = dbcSettings.GetSettings().DBCVersion;
            
            Save = new DelegateCommand(() =>
            {
                dbcSettings.UpdateSettings(new DBCSettings {Path = Path, SkipLoading = SkipLoading, DBCVersion = DBCVersion});
                IsModified = false;
            });
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

        private bool isModified;
        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }
        
        public string Name => "DBC";

        public ICommand Save { get; }
    }
}