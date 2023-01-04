using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonAzerothString : ITrinityString
    {
        
        public uint Entry { get; set; }

         public string ContentDefault { get; set; } = "";
         public string? ContentLocKo { get; set; }
         public string? ContentLocFr { get; set; }
         public string? ContentLocDe { get; set; }
         public string? ContentLocZhCn { get; set; }
         public string? ContentLocZhTw { get; set; }
         public string? ContentLocEsEs { get; set; }
         public string? ContentLocEsMx { get; set; }
         public string? ContentLocRu { get; set; }

        public int LocalesCount => 8;

        public string? this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return ContentDefault;
                    case 1: return ContentLocKo;
                    case 2: return ContentLocFr;
                    case 3: return ContentLocDe;
                    case 4: return ContentLocZhCn;
                    case 5: return ContentLocZhTw;
                    case 6: return ContentLocEsEs;
                    case 7: return ContentLocEsMx;
                    case 8: return ContentLocRu;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }
}