
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonAreaTriggerTemplate : IAreaTriggerTemplate
    {
        
        public uint Id { get; set; }

        
        public bool IsServerSide { get; set; }

        public string? Name => null;
        public string? ScriptName => null;
        public string? AIName => null;
    }
}