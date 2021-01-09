using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "gameobject_template")]
    public class MySqlGameObjectTemplate : IGameObjectTemplate
    {
        [PrimaryKey]
        [Column(Name = "entry")]
        public uint Entry { get; set; }

        [Column(Name = "name")]
        public string Name { get; set; } = "";
    }
}
