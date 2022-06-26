﻿using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.MySqlDatabaseCommon.Providers;

namespace WDE.CMMySqlDatabase.Data
{
    public struct DbAccess : ISettings, IDbAccess
    {
        public static DbAccess Default => new() {Host = "localhost", Port = 3306};
        
        public string? Host { get; set; }
        public string? Password { get; set; }
        public int? Port { get; set; }
        public string? User { get; set; }
        public string? Database { get; set; }
        
        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Database) || string.IsNullOrEmpty(User);
    }
}