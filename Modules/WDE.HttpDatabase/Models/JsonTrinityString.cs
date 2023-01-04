using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonTrinityString : ITrinityString
    {
        
        public uint Entry { get; set; }

         public string ContentDefault { get; set; } = "";
         public string? ContentLoc1 { get; set; }
         public string? ContentLoc2 { get; set; }
         public string? ContentLoc3 { get; set; }
         public string? ContentLoc4 { get; set; }
         public string? ContentLoc5 { get; set; }
         public string? ContentLoc6 { get; set; }
         public string? ContentLoc7 { get; set; }
         public string? ContentLoc8 { get; set; }

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