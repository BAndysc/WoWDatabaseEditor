namespace WDE.Common.Database
{
    public interface INpcText
    {
        uint Id { get; }
        uint BroadcastTextId { get; }
        string? Text0_0 { get; }
        string? Text0_1 { get; }
    }

    public interface INpcTextFull
    {
        uint Id { get; }
        string? Text0_0 { get; }
        string? Text0_1 { get; }
        int BroadcastTextID0 { get; }
        byte Lang0 { get; }
        float Probability0 { get; }
        uint EmoteDelay0_0 { get; }
        uint Emote0_0 { get; }
        uint EmoteDelay0_1 { get; }
        uint Emote0_1 { get; }
        uint EmoteDelay0_2 { get; }
        uint Emote0_2 { get; }
        string? Text1_0 { get; }
        string? Text1_1 { get; }
        int BroadcastTextID1 { get; }
        byte Lang1 { get; }
        float Probability1 { get; }
        uint EmoteDelay1_0 { get; }
        uint Emote1_0 { get; }
        uint EmoteDelay1_1 { get; }
        uint Emote1_1 { get; }
        uint EmoteDelay1_2 { get; }
        uint Emote1_2 { get; }
        string? Text2_0 { get; }
        string? Text2_1 { get; }
        int BroadcastTextID2 { get; }
        byte Lang2 { get; }
        float Probability2 { get; }
        uint EmoteDelay2_0 { get; }
        uint Emote2_0 { get; }
        uint EmoteDelay2_1 { get; }
        uint Emote2_1 { get; }
        uint EmoteDelay2_2 { get; }
        uint Emote2_2 { get; }
        string? Text3_0 { get; }
        string? Text3_1 { get; }
        int BroadcastTextID3 { get; }
        byte Lang3 { get; }
        float Probability3 { get; }
        uint EmoteDelay3_0 { get; }
        uint Emote3_0 { get; }
        uint EmoteDelay3_1 { get; }
        uint Emote3_1 { get; }
        uint EmoteDelay3_2 { get; }
        uint Emote3_2 { get; }
        string? Text4_0 { get; }
        string? Text4_1 { get; }
        int BroadcastTextID4 { get; }
        byte Lang4 { get; }
        float Probability4 { get; }
        uint EmoteDelay4_0 { get; }
        uint Emote4_0 { get; }
        uint EmoteDelay4_1 { get; }
        uint Emote4_1 { get; }
        uint EmoteDelay4_2 { get; }
        uint Emote4_2 { get; }
        string? Text5_0 { get; }
        string? Text5_1 { get; }
        int BroadcastTextID5 { get; }
        byte Lang5 { get; }
        float Probability5 { get; }
        uint EmoteDelay5_0 { get; }
        uint Emote5_0 { get; }
        uint EmoteDelay5_1 { get; }
        uint Emote5_1 { get; }
        uint EmoteDelay5_2 { get; }
        uint Emote5_2 { get; }
        string? Text6_0 { get; }
        string? Text6_1 { get; }
        int BroadcastTextID6 { get; }
        byte Lang6 { get; }
        float Probability6 { get; }
        uint EmoteDelay6_0 { get; }
        uint Emote6_0 { get; }
        uint EmoteDelay6_1 { get; }
        uint Emote6_1 { get; }
        uint EmoteDelay6_2 { get; }
        uint Emote6_2 { get; }
        string? Text7_0 { get; }
        string? Text7_1 { get; }
        int BroadcastTextID7 { get; }
        byte Lang7 { get; }
        float Probability7 { get; }
        uint EmoteDelay7_0 { get; }
        uint Emote7_0 { get; }
        uint EmoteDelay7_1 { get; }
        uint Emote7_1 { get; }
        uint EmoteDelay7_2 { get; }
        uint Emote7_2 { get; }
        int VerifiedBuild { get; }
    }

    public class AbstractNpcText : INpcText
    {
        public uint Id { get; init; }
        public uint BroadcastTextId { get; init; }
        public string? Text0_0 { get; init; }
        public string? Text0_1 { get; init; }
    }
    
    public class AbstractNpcTextFull : INpcTextFull
    {
        public uint Id { get; set; }
        public string? Text0_0 { get; set; }
        public string? Text0_1 { get; set; }
        public int BroadcastTextID0 { get; set; }
        public byte Lang0 { get; set; }
        public float Probability0 { get; set; }
        public uint EmoteDelay0_0 { get; set; }
        public uint Emote0_0 { get; set; }
        public uint EmoteDelay0_1 { get; set; }
        public uint Emote0_1 { get; set; }
        public uint EmoteDelay0_2 { get; set; }
        public uint Emote0_2 { get; set; }
        public string? Text1_0 { get; set; }
        public string? Text1_1 { get; set; }
        public int BroadcastTextID1 { get; set; }
        public byte Lang1 { get; set; }
        public float Probability1 { get; set; }
        public uint EmoteDelay1_0 { get; set; }
        public uint Emote1_0 { get; set; }
        public uint EmoteDelay1_1 { get; set; }
        public uint Emote1_1 { get; set; }
        public uint EmoteDelay1_2 { get; set; }
        public uint Emote1_2 { get; set; }
        public string? Text2_0 { get; set; }
        public string? Text2_1 { get; set; }
        public int BroadcastTextID2 { get; set; }
        public byte Lang2 { get; set; }
        public float Probability2 { get; set; }
        public uint EmoteDelay2_0 { get; set; }
        public uint Emote2_0 { get; set; }
        public uint EmoteDelay2_1 { get; set; }
        public uint Emote2_1 { get; set; }
        public uint EmoteDelay2_2 { get; set; }
        public uint Emote2_2 { get; set; }
        public string? Text3_0 { get; set; }
        public string? Text3_1 { get; set; }
        public int BroadcastTextID3 { get; set; }
        public byte Lang3 { get; set; }
        public float Probability3 { get; set; }
        public uint EmoteDelay3_0 { get; set; }
        public uint Emote3_0 { get; set; }
        public uint EmoteDelay3_1 { get; set; }
        public uint Emote3_1 { get; set; }
        public uint EmoteDelay3_2 { get; set; }
        public uint Emote3_2 { get; set; }
        public string? Text4_0 { get; set; }
        public string? Text4_1 { get; set; }
        public int BroadcastTextID4 { get; set; }
        public byte Lang4 { get; set; }
        public float Probability4 { get; set; }
        public uint EmoteDelay4_0 { get; set; }
        public uint Emote4_0 { get; set; }
        public uint EmoteDelay4_1 { get; set; }
        public uint Emote4_1 { get; set; }
        public uint EmoteDelay4_2 { get; set; }
        public uint Emote4_2 { get; set; }
        public string? Text5_0 { get; set; }
        public string? Text5_1 { get; set; }
        public int BroadcastTextID5 { get; set; }
        public byte Lang5 { get; set; }
        public float Probability5 { get; set; }
        public uint EmoteDelay5_0 { get; set; }
        public uint Emote5_0 { get; set; }
        public uint EmoteDelay5_1 { get; set; }
        public uint Emote5_1 { get; set; }
        public uint EmoteDelay5_2 { get; set; }
        public uint Emote5_2 { get; set; }
        public string? Text6_0 { get; set; }
        public string? Text6_1 { get; set; }
        public int BroadcastTextID6 { get; set; }
        public byte Lang6 { get; set; }
        public float Probability6 { get; set; }
        public uint EmoteDelay6_0 { get; set; }
        public uint Emote6_0 { get; set; }
        public uint EmoteDelay6_1 { get; set; }
        public uint Emote6_1 { get; set; }
        public uint EmoteDelay6_2 { get; set; }
        public uint Emote6_2 { get; set; }
        public string? Text7_0 { get; set; }
        public string? Text7_1 { get; set; }
        public int BroadcastTextID7 { get; set; }
        public byte Lang7 { get; set; }
        public float Probability7 { get; set; }
        public uint EmoteDelay7_0 { get; set; }
        public uint Emote7_0 { get; set; }
        public uint EmoteDelay7_1 { get; set; }
        public uint Emote7_1 { get; set; }
        public uint EmoteDelay7_2 { get; set; }
        public uint Emote7_2 { get; set; }
        public int VerifiedBuild { get; set; }
    }
}