using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table("broadcast_text_locale")]
public class MySqlBroadcastTextLocale : IBroadcastTextLocale
{
    [PrimaryKey]
    [Column(Name = "ID")]
    public uint Id { get; set; }
        
    [PrimaryKey]
    [Column(Name = "locale")]
    public string Locale { get; set; } = "";
        
    [Column(Name = "Text")]
    public string? Text { get; set; }
        
    [Column(Name = "Text1")]
    public string? Text1 { get; set; }
}
