using Newtonsoft.Json;
using WDE.Common.Services;

namespace WDE.TrinityMySqlDatabase.Data
{
    public class DbAccess : ISettings
    {
        public string? Host { get; set; }
        public string? Password { get; set; }
        public int? Port { get; set; } = 3306;
        public string? User { get; set; }
        public string? Database { get; set; }
        
        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Database) || string.IsNullOrEmpty(User);
    }
}