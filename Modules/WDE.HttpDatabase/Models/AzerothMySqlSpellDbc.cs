
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class AzerothJsonSpellDbc : IDatabaseSpellDbc
    {
        
        public uint Id { get; set; }

        
        public string? Name { get; set; }
    }
}