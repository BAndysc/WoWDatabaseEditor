using LinqToDB.Mapping;
using System;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    [Table(Name = "mangos_string")]
    public class CmangosString : ITrinityString
    {
        [PrimaryKey]
        [Column(Name = "entry")]
        public uint Entry { get; set; }

        [Column(Name = "content_default")] public string ContentDefault { get; set; } = "";
        [Column(Name = "content_loc1")] public string? ContentLoc1 { get; set; }
        [Column(Name = "content_loc2")] public string? ContentLoc2 { get; set; }
        [Column(Name = "content_loc3")] public string? ContentLoc3 { get; set; }
        [Column(Name = "content_loc4")] public string? ContentLoc4 { get; set; }
        [Column(Name = "content_loc5")] public string? ContentLoc5 { get; set; }
        [Column(Name = "content_loc6")] public string? ContentLoc6 { get; set; }
        [Column(Name = "content_loc7")] public string? ContentLoc7 { get; set; }
        [Column(Name = "content_loc8")] public string? ContentLoc8 { get; set; }

        public int LocalesCount => 8;

        public string? this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return ContentDefault;
                    case 1: return ContentLoc1;
                    case 2: return ContentLoc2;
                    case 3: return ContentLoc3;
                    case 4: return ContentLoc4;
                    case 5: return ContentLoc5;
                    case 6: return ContentLoc6;
                    case 7: return ContentLoc7;
                    case 8: return ContentLoc8;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
