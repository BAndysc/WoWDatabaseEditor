
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class TrinityJsonSpellDbc : IDatabaseSpellDbc
    {
        
        public uint Id { get; set; }

        
        public string? Name { get; set; }
    }
    

    public class TrinityMasterJsonServersideSpell : IDatabaseSpellDbc
    {
        
        public uint Id { get; set; }

        
        public string? Name { get; set; }
    }
}