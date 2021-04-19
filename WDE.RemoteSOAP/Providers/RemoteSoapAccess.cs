using WDE.Common.Services;

namespace WDE.RemoteSOAP.Providers
{
    public class RemoteSoapAccess : ISettings
    {
        public string? Host { get; set; }
        public int? Port { get; set; } = 7878;
        public string? User { get; set; }
        public string? Password { get; set; }
        public bool IsEmpty => string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(User) || string.IsNullOrEmpty(Password);
    }
}