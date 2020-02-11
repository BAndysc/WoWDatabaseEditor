using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WDE.DbcStore.Providers;

namespace WDE.DbcStore.ViewModels
{
    public class DBCConfigViewModel : BindableBase
    {
        private readonly IDbcSettingsProvider dbcSettings;

        public Action SaveAction { get; set; }
        
        private string _path;
        public string Path
        {
            get { return _path; }
            set { SetProperty(ref _path, value); }
        }

        private bool _skipLoading;
        public bool SkipLoading
        {
            get { return _skipLoading; }
            set { SetProperty(ref _skipLoading, value); }
        }

        private DBCVersions _dbcVersion;
        public DBCVersions DBCVersion
        {
            get { return _dbcVersion; }
            set { SetProperty(ref _dbcVersion, value); }
        }

        public DBCConfigViewModel(IDbcSettingsProvider dbcSettings)
        {
            SaveAction = Save;
            Path = dbcSettings.GetSettings().Path;
            SkipLoading = dbcSettings.GetSettings().SkipLoading;
            DBCVersion = dbcSettings.GetSettings().DBCVersion;
            this.dbcSettings = dbcSettings;
        }

        private void Save()
        {
            dbcSettings.UpdateSettings(new Data.DBCSettings() { Path = Path, SkipLoading = SkipLoading, DBCVersion = DBCVersion });
        }
    }
}
