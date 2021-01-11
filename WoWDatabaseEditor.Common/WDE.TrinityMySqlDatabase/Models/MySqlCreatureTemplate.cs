using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name="creature_template")]
    public class MySqlCreatureTemplate : ICreatureTemplate
    {
        [PrimaryKey, Identity]
        [Column(Name="entry")]
        public uint Entry { get; set; }

        [Column(Name = "name")] 
        public string? Name { get; set; }
        
        [Column(Name = "AIName")] 
        public string? AIName { get; set; }
        
        [Column(Name = "ScriptName")] 
        public string? ScriptName { get; set; }
    }
}
