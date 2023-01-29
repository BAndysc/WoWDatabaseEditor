using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models;

[Table(Name = "phase_name")]
public class MySqlPhaseName : IPhaseName
{
    [PrimaryKey]
    [Column(Name = "ID")]
    public uint Id { get; set; }

    public string Name => _Name ?? "";

    [Column(Name = "Name")]
    public string? _Name { get; set; } = "";
}