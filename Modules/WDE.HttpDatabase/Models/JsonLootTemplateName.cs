using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;

public class JsonLootTemplateName : ILootTemplateName
{
    public uint Entry { get; set; }
    public string Name { get; set; } = "";
    public bool DontLoadRecursively { get; set; }
}