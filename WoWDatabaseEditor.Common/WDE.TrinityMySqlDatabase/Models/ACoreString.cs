using System;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "acore_string")]
    public class ACoreString : ITrinityString
    {
        [PrimaryKey]
        [Column(Name = "entry")]
        public uint Entry { get; set; }

        [Column(Name = "content_default")] public string ContentDefault { get; set; } = "";
        [Column(Name = "locale_koKR")] public string? ContentLocKo { get; set; }
        [Column(Name = "locale_frFR")] public string? ContentLocFr { get; set; }
        [Column(Name = "locale_deDE")] public string? ContentLocDe { get; set; }
        [Column(Name = "locale_zhCN")] public string? ContentLocZhCn { get; set; }
        [Column(Name = "locale_zhTW")] public string? ContentLocZhTw { get; set; }
        [Column(Name = "locale_esES")] public string? ContentLocEsEs { get; set; }
        [Column(Name = "locale_esMX")] public string? ContentLocEsMx { get; set; }
        [Column(Name = "locale_ruRU")] public string? ContentLocRu { get; set; }

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