
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public class JsonPhaseName : IPhaseName
{
    
    
    public uint Id { get; set; }

    public string Name => _Name ?? "";

    
    public string? _Name { get; set; } = "";
}