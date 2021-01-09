using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.TrinityMySqlDatabase.Data
{
    public class DbAccess
    {
        public string? Host { get; set; }
        public string? Password { get; set; }
        public int? Port { get; set; } = 3306;
        public string? User { get; set; }
        public string? Database { get; set; }
    }
}
