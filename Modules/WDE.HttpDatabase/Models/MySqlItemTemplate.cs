
using WDE.Common.DBC;

namespace WDE.HttpDatabase.Models;


public class JsonItemTemplate : IItem
{
    
    
    public uint Entry { get; set; }

     
    public string Name { get; set; } = "";
    
     
    public uint DisplayId { get; set; }
}