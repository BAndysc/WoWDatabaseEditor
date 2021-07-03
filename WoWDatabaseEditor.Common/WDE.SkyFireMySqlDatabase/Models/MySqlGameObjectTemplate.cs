using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.SkyFireMySqlDatabase.Models
{
    [Table(Name = "gameobject_template")]
    public class MySqlGameObjectTemplate : IGameObjectTemplate
    {
        [PrimaryKey]
        [Column(Name = "entry")]
        public uint Entry { get; set; }

        [Column(Name = "name")]
        public string Name { get; set; } = "";

        [Column(Name = "AIName")] 
        public string AIName { get; set; } = "";

        [Column(Name = "ScriptName")] 
        public string ScriptName { get; set; } = "";
    }
}