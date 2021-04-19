namespace WDE.RemoteSOAP.Providers
{
    public interface IConnectionSettingsProvider
    {
        RemoteSoapAccess GetSettings();
        void UpdateSettings(string? user, string? password, string? host, int? port);
    }
}