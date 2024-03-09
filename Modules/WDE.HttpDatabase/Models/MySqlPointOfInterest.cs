
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonPointOfInterest : IPointOfInterest
    {
        
        public uint Id { get; set; }
        
        
        public float PositionX { get; set; }
        
        
        public float PositionY { get; set; }
        
        
        public uint Icon { get; set; }
        
        
        public uint Flags { get; set; }
        
        
        public uint Importance { get; set; }
        
        
        public string Name { get; set; } = "";
        
        
        public int? VerifiedBuild { get; set; }
    }
}