using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models.TBC
{
    [Table(Name = "gameobject_template")]
    public class GameObjectTemplateTBC : IGameObjectTemplate
    {
        [Column("entry"         , IsPrimaryKey = true )] public uint   Entry          { get; set; } // mediumint(8) unsigned
        [Column("name"          , CanBeNull    = false)] public string Name           { get; set; } = null!; // varchar(100)
        [Column("type"                                )] public GameobjectType   Type           { get; set; } // tinyint(3) unsigned
        [Column("ScriptName"    , CanBeNull    = false)] public string ScriptName     { get; set; } = null!; // varchar(64)
        [Column("data0"                               )] public uint   Data0          { get; set; } // int(10) unsigned
        [Column("data1"                               )] public int    Data1          { get; set; } // int(10)
        [Column("size"                                )] public float  Size           { get; set; } // float
        [Column("displayId"                           )] public uint   DisplayId      { get; set; } // mediumint(8) unsigned
        [Column(Name = "flags"                                   )] public uint   Flags          { get; set; }

        public uint this[int dataIndex]
        {
            get
            {
                switch (dataIndex)
                {
                    case 0: return Data0;
                    case 1: return (uint)Data1;
                    default: return 0;
                }
            }
        }

        public uint DataCount => 2;
        
        // not implemented in cmangos
        public string AIName { get; set; } = null!;
    }
}