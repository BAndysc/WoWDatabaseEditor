
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;

public class JsonBroadcastTextLocale : IBroadcastTextLocale
{
    
    
    public uint Id { get; set; }
        
    
    
    public string Locale { get; set; } = "";
        
    
    public string? Text { get; set; }
        
    
    public string? Text1 { get; set; }
}
