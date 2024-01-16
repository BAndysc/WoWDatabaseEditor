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

        [Column(Name = "displayId")]
        public uint DisplayId { get; set; }

        [Column(Name = "size")]
        public float Size { get; set; }

        [Column(Name = "type")]
        public GameobjectType Type { get; set; }

        public uint Flags => 0;

        [Column(Name = "name")]
        public string Name { get; set; } = "";

        [Column(Name = "AIName")] 
        public string AIName { get; set; } = "";

        [Column(Name = "ScriptName")] 
        public string ScriptName { get; set; } = "";

        [Column(Name = "Data0")]
        public int Data0 { get; set; }

        [Column(Name = "Data1")]
        public int Data1 { get; set; }

        public uint this[int dataIndex]
        {
            get
            {
                if (dataIndex == 0)
                    return Data0 < 0 ? 0 : (uint)Data0;
                if (dataIndex == 1)
                    return Data1 < 0 ? 0 : (uint)Data1;
                return 0;
            }
        }
        
        public uint DataCount => 2;
    }
}