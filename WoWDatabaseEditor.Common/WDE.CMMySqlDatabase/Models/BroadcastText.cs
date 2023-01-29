using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
        /// <summary>
    /// Broadcast Text System
    /// </summary>
    [Table("broadcast_text")]
    public class BroadcastTextWoTLK : IBroadcastText
    {
        /// <summary>
        /// Identifier
        /// </summary>
        [Column("Id"             , IsPrimaryKey = true)] public int     _Id             { get; set; } // int(11)
        /// <summary>
        /// Male text
        /// </summary>
        [Column("Text"                                )] public string? Text            { get; set; } // text
        /// <summary>
        /// Female text
        /// </summary>
        [Column("Text1"                               )] public string? Text1           { get; set; } // text
        [Column("ChatTypeID"                          )] public int     ChatTypeId      { get; set; } // int(11)
        /// <summary>
        /// Language of text
        /// </summary>
        [Column("LanguageID"                          )] public int     LanguageId      { get; set; } // int(11)
        /// <summary>
        /// Unk
        /// </summary>
        [Column("ConditionID"                         )] public int     ConditionId     { get; set; } // int(11)
        /// <summary>
        /// Unk
        /// </summary>
        [Column("EmotesID"                            )] public int     _EmotesId       { get; set; } // int(11)
        /// <summary>
        /// Unk
        /// </summary>
        [Column("Flags"                               )] public int     _Flags          { get; set; } // int(11)
        /// <summary>
        /// Sound on broadcast
        /// </summary>
        [Column("SoundEntriesID1"                     )] public int     _SoundEntriesId1 { get; set; } // int(11)
        /// <summary>
        /// Sound on broadcast
        /// </summary>
        [Column("SoundEntriesID2"                     )] public int     SoundEntriesId2 { get; set; } // int(11)
        /// <summary>
        /// Emote on gossip
        /// </summary>
        [Column("EmoteID1"                            )] public int     _EmoteId1       { get; set; } // int(11)
        /// <summary>
        /// Emote on gossip
        /// </summary>
        [Column("EmoteID2"                            )] public int     _EmoteId2       { get; set; } // int(11)
        /// <summary>
        /// Emote on gossip
        /// </summary>
        [Column("EmoteID3"                            )] public int     _EmoteId3       { get; set; } // int(11)
        /// <summary>
        /// Emote delay on gossip
        /// </summary>
        [Column("EmoteDelay1"                         )] public int     _EmoteDelay1    { get; set; } // int(11)
        /// <summary>
        /// Emote delay on gossip
        /// </summary>
        [Column("EmoteDelay2"                         )] public int     _EmoteDelay2    { get; set; } // int(11)
        /// <summary>
        /// Emote delay on gossip
        /// </summary>
        [Column("EmoteDelay3"                         )] public int     _EmoteDelay3    { get; set; } // int(11)
        /// <summary>
        /// Build of bruteforce
        /// </summary>
        [Column("VerifiedBuild"                       )] public int     VerifiedBuild   { get; set; } // int(11)


        public uint Id
        {
            get => _Id > 0 ? (uint)_Id : 0;
            set { _Id = (int)value; }
        }
        public uint Language
        {
            get => LanguageId > 0 ? (uint)LanguageId : 0;
            set { LanguageId = (int)value; }
        }

        public uint EmotesId
        {
            get => _EmotesId > 0 ? (uint)_EmotesId : 0;
            set { _EmotesId = (int)value; }
        }

        public uint EmoteId1
        {
            get => _EmoteId1 > 0 ? (uint)_EmoteId1 : 0;
            set { _EmoteId1 = (int)value; }
        }

        public uint EmoteId2
        {
            get => _EmoteId2 > 0 ? (uint)_EmoteId2 : 0;
            set { _EmoteId2 = (int)value; }
        }

        public uint EmoteId3
        {
            get => _EmoteId3 > 0 ? (uint)_EmoteId3 : 0;
            set { _EmoteId3 = (int)value; }
        }

        public uint EmoteDelay1
        {
            get => _EmoteDelay1 > 0 ? (uint)_EmoteDelay1 : 0;
            set { _EmoteDelay1 = (int)value; }
        }

        public uint EmoteDelay2
        {
            get => _EmoteDelay2 > 0 ? (uint)_EmoteDelay2 : 0;
            set { _EmoteDelay2 = (int)value; }
        }

        public uint EmoteDelay3
        {
            get => _EmoteDelay3 > 0 ? (uint)_EmoteDelay3 : 0;
            set { _EmoteDelay3 = (int)value; }
        }

        public uint Sound1 { get; set; }
        public uint Sound2 { get; set; }

        public uint SoundEntriesId
        {
            get => _SoundEntriesId1 > 0 ? (uint)_SoundEntriesId1 : 0;
            set { _SoundEntriesId1 = (int)value; }
        }

        public uint Flags
        {
            get => _Flags > 0 ? (uint)_Flags : 0;
            set { _Flags = (int)_Flags; }
        }

    }
    
    [Table("broadcast_text_locale")]
    public class BroadcastTextLocale : IBroadcastTextLocale
    {
        [PrimaryKey]
        [Column(Name = "Id")]
        public uint Id { get; set; }
    
        [PrimaryKey]
        [Column(Name = "Locale")]
        public string Locale { get; set; } = "";
    
        [Column(Name = "Text_lang")]
        public string? Text { get; set; }
    
        [Column(Name = "Text1_lang")]
        public string? Text1 { get; set; }
    }
}