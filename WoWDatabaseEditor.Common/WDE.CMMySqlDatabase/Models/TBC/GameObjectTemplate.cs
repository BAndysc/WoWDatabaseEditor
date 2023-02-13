using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models.TBC
{
    [Table(Name = "gameobject_template")]
    public class GameObjectTemplateTBC : IGameObjectTemplate
    {
        [Column("entry"         , IsPrimaryKey = true )] public uint   Entry          { get; set; } // mediumint(8) unsigned
        [Column("name"          , CanBeNull    = false)] public string Name           { get; set; } = null!; // varchar(100)
        [Column("type"                                )] public byte   Type_           { get; set; } // tinyint(3) unsigned
        [Column("ScriptName"    , CanBeNull    = false)] public string ScriptName     { get; set; } = null!; // varchar(64)

        public uint this[int dataIndex] => 0;

        public uint DataCount => 0;
        [Column("size"                                )] public float  Size           { get; set; } // float
        [Column("displayId"                           )] public uint   DisplayId      { get; set; } // mediumint(8) unsigned

        // not implemented in cmangos
        public string AIName { get; set; } = null!;
        public GameobjectType Type => (GameobjectType)Type_;
    }
}