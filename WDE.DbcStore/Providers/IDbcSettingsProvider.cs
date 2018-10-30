using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.DbcStore.Data;

namespace WDE.DbcStore.Providers
{
    public interface IDbcSettingsProvider
    {
        DBCSettings GetSettings();
        void UpdateSettings(DBCSettings newSettings);
    }
}
