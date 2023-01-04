
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonCoreCommandHelp : ICoreCommandHelp
    { 
         
        public string Name { get; set; } = "";
        
        
        public string? Help { get; set;}
    }
}