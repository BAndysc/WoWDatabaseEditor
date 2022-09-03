using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models.Wrath
{
    [Table(Name = "gameobject_template")]
    public class GameObjectTemplateWoTLK : IGameObjectTemplate
    {
        [Column("entry"         , IsPrimaryKey = true )] public uint   Entry          { get; set; } // mediumint(8) unsigned
        [Column("type"                                )] public byte   Type_           { get; set; } // tinyint(3) unsigned
        [Column("displayId"                           )] public uint   DisplayId      { get; set; } // mediumint(8) unsigned
        [Column("name"          , CanBeNull    = false)] public string Name           { get; set; } = null!; // varchar(100)
        [Column("IconName"      , CanBeNull    = false)] public string IconName       { get; set; } = null!; // varchar(100)
        [Column("castBarCaption", CanBeNull    = false)] public string CastBarCaption { get; set; } = null!; // varchar(100)
        [Column("unk1"          , CanBeNull    = false)] public string Unk1           { get; set; } = null!; // varchar(100)
        [Column("faction"                             )] public ushort Faction        { get; set; } // smallint(5) unsigned
        [Column("flags"                               )] public uint   Flags          { get; set; } // int(10) unsigned
        [Column("ExtraFlags"                          )] public uint   ExtraFlags     { get; set; } // int(11) unsigned
        [Column("size"                                )] public float  Size           { get; set; } // float
        [Column("questItem1"                          )] public uint   QuestItem1     { get; set; } // int(11) unsigned
        [Column("questItem2"                          )] public uint   QuestItem2     { get; set; } // int(11) unsigned
        [Column("questItem3"                          )] public uint   QuestItem3     { get; set; } // int(11) unsigned
        [Column("questItem4"                          )] public uint   QuestItem4     { get; set; } // int(11) unsigned
        [Column("questItem5"                          )] public uint   QuestItem5     { get; set; } // int(11) unsigned
        [Column("questItem6"                          )] public uint   QuestItem6     { get; set; } // int(11) unsigned
        [Column("data0"                               )] public uint   Data0          { get; set; } // int(10) unsigned
        [Column("data1"                               )] public int    Data1          { get; set; } // int(10)
        [Column("data2"                               )] public uint   Data2          { get; set; } // int(10) unsigned
        [Column("data3"                               )] public uint   Data3          { get; set; } // int(10) unsigned
        [Column("data4"                               )] public uint   Data4          { get; set; } // int(10) unsigned
        [Column("data5"                               )] public uint   Data5          { get; set; } // int(10) unsigned
        [Column("data6"                               )] public int    Data6          { get; set; } // int(10)
        [Column("data7"                               )] public uint   Data7          { get; set; } // int(10) unsigned
        [Column("data8"                               )] public uint   Data8          { get; set; } // int(10) unsigned
        [Column("data9"                               )] public uint   Data9          { get; set; } // int(10) unsigned
        [Column("data10"                              )] public uint   Data10         { get; set; } // int(10) unsigned
        [Column("data11"                              )] public uint   Data11         { get; set; } // int(10) unsigned
        [Column("data12"                              )] public uint   Data12         { get; set; } // int(10) unsigned
        [Column("data13"                              )] public uint   Data13         { get; set; } // int(10) unsigned
        [Column("data14"                              )] public uint   Data14         { get; set; } // int(10) unsigned
        [Column("data15"                              )] public uint   Data15         { get; set; } // int(10) unsigned
        [Column("data16"                              )] public uint   Data16         { get; set; } // int(10) unsigned
        [Column("data17"                              )] public uint   Data17         { get; set; } // int(10) unsigned
        [Column("data18"                              )] public uint   Data18         { get; set; } // int(10) unsigned
        [Column("data19"                              )] public uint   Data19         { get; set; } // int(10) unsigned
        [Column("data20"                              )] public uint   Data20         { get; set; } // int(10) unsigned
        [Column("data21"                              )] public uint   Data21         { get; set; } // int(10) unsigned
        [Column("data22"                              )] public uint   Data22         { get; set; } // int(10) unsigned
        [Column("data23"                              )] public uint   Data23         { get; set; } // int(10) unsigned
        [Column("CustomData1"                         )] public uint   CustomData1    { get; set; } // int(11) unsigned
        [Column("mingold"                             )] public uint   Mingold        { get; set; } // mediumint(8) unsigned
        [Column("maxgold"                             )] public uint   Maxgold        { get; set; } // mediumint(8) unsigned
        [Column("ScriptName"    , CanBeNull    = false)] public string ScriptName     { get; set; } = null!; // varchar(64)

        // not implemented in cmangos
        public string AIName { get; set; } = null!;
        public GameobjectType Type => (GameobjectType)Type_;
    }
}