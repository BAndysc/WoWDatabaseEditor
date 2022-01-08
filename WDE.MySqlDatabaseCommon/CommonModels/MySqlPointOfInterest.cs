using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels
{
    [Table(Name = "points_of_interest")]
    public class MySqlPointOfInterest : IPointOfInterest
    {
        [PrimaryKey]
        [Column(Name = "ID")]
        public uint Id { get; set; }
        
        [Column(Name = "PositionX")]
        public float PositionX { get; set; }
        
        [Column(Name = "PositionY")]
        public float PositionY { get; set; }
        
        [Column(Name = "Icon")]
        public uint Icon { get; set; }
        
        [Column(Name = "Flags")]
        public uint Flags { get; set; }
        
        [Column(Name = "Importance")]
        public uint Importance { get; set; }
        
        [Column(Name = "Name")]
        public string Name { get; set; } = "";
        
        [Column(Name = "VerifiedBuild")]
        public int? VerifiedBuild { get; set; }
    }
}