using System.IO;
using System.Windows;
using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.RemoteSOAP.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class ConnectionSettingsProvider : IConnectionSettingsProvider
    {
        private readonly IUserSettings userSettings;

        public ConnectionSettingsProvider(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            SoapAccess = userSettings.Get(new RemoteSoapAccess());
        }

        private RemoteSoapAccess SoapAccess { get; }

        public RemoteSoapAccess GetSettings() => SoapAccess;

        public void UpdateSettings(string? user, string? password, string? host, int? port)
        {
            SoapAccess.User = user;
            SoapAccess.Password = password;
            SoapAccess.Host = host;
            SoapAccess.Port = port;
            userSettings.Update(SoapAccess);
        }
    }
}