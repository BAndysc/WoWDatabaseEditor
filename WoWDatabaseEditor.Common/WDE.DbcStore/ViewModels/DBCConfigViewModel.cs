using System;
using Prism.Mvvm;
using WDE.DbcStore.Data;
using WDE.DbcStore.Providers;

namespace WDE.DbcStore.ViewModels
{
    public class DBCConfigViewModel : BindableBase
    {
        private readonly IDbcSettingsProvider dbcSettings;

        private DBCVersions dbcVersion;

        private string path;

        private bool skipLoading;

        public DBCConfigViewModel(IDbcSettingsProvider dbcSettings)
        {
            SaveAction = Save;
            Path = dbcSettings.GetSettings().Path;
            SkipLoading = dbcSettings.GetSettings().SkipLoading;
            DBCVersion = dbcSettings.GetSettings().DBCVersion;
            this.dbcSettings = dbcSettings;
        }

        public Action SaveAction { get; set; }

        public string Path
        {
            get => path;
            set => SetProperty(ref path, value);
        }

        public bool SkipLoading
        {
            get => skipLoading;
            set => SetProperty(ref skipLoading, value);
        }

        public DBCVersions DBCVersion
        {
            get => dbcVersion;
            set => SetProperty(ref dbcVersion, value);
        }

        private void Save()
        {
            dbcSettings.UpdateSettings(new DBCSettings {Path = Path, SkipLoading = SkipLoading, DBCVersion = DBCVersion});
        }
    }
}