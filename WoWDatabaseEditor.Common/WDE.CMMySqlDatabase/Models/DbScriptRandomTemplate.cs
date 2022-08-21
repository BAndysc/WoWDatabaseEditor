using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "dbscript_random_templates")]
public class DbScriptRandomTemplate : IDbScriptRandomTemplate
{
    [PrimaryKey]
    [Identity]
    [Column("id")]
    public uint Id { get; set; }
    
    [PrimaryKey]
    [Identity]
    [Column("type")]
    public uint Type { get; set; }
    
    [PrimaryKey]
    [Identity]
    [Column("target_id")]
    public int Value { get; set; }
    
    [Column("chance")]
    public int Chance { get; set; }
    
    [Column("comments")]
    public string? Comment { get; set; }
}