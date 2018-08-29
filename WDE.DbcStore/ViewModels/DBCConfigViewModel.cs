using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WDE.DbcStore.ViewModels
{
    public class DBCConfigViewModel : BindableBase
    {
        public Action SaveAction { get; set; }

        private string _path;

        public string Path
        {
            get { return _path; }
            set { SetProperty(ref _path, value); }
        }
        
        public DBCConfigViewModel()
        {
            SaveAction = Save;
            Path = DbcStoreModule.DBCSettings.Path;
        }

        private void Save()
        {
            DbcStoreModule.DBCSettings.Path = Path;
            JsonSerializer ser = new JsonSerializer() { TypeNameHandling = TypeNameHandling.Auto };
            using (StreamWriter file = File.CreateText(@"dbc.json"))
            {
                ser.Serialize(file, DbcStoreModule.DBCSettings);
            }
            MessageBox.Show("Restart the application.");
        }
    }
}
