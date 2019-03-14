using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.TrinityMySqlDatabase.Data;

namespace WDE.TrinityMySqlDatabase.Providers
{
    public interface IConnectionSettingsProvider
    {
        DbAccess GetSettings();
        void UpdateSettings(string user, string password, string host, string database);
    }
}
