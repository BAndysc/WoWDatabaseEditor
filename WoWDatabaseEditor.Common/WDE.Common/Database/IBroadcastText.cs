namespace WDE.Common.Database
{
    public interface IBroadcastText
    {
        uint Id { get; }
        uint Language { get; }
        string? Text { get; }
        string? Text1 { get; }
        uint EmoteId1  { get; }
        uint EmoteId2 { get; }
        uint EmoteId3 { get; }
        uint EmoteDelay1 { get; }
        uint EmoteDelay2 { get; }
        uint EmoteDelay3 { get; }
        uint Sound1 { get; }
        uint Sound2 { get; }
        uint SoundEntriesId { get; }
        uint EmotesId { get; }
        uint Flags { get; }

        int ChatTypeId => 0; // used in mangos, but helpful there
    }

    public static class BroadcastTextExtensions
    {
        public static string? FirstText(this IBroadcastText self)
        {
            if (!string.IsNullOrEmpty(self.Text))
                return self.Text;
            return self.Text1;
        }
    }
    
    public interface IBroadcastTextLocale
    {
        uint Id { get; }
        string Locale { get; }
        string? Text { get; }
        string? Text1 { get; }
    }

    public class AbstractBroadcastText : IBroadcastText
    {
        public uint Id { get; init; }
        public uint Language { get; init; }
        public string? Text { get; init; }
        public string? Text1 { get; init; }
        public uint EmoteId1 { get; init; }
        public uint EmoteId2 { get; init; }
        public uint EmoteId3 { get; init; }
        public uint EmoteDelay1 { get; init; }
        public uint EmoteDelay2 { get; init; }
        public uint EmoteDelay3 { get; init; }
        public uint Sound1 { get; init; }
        public uint Sound2 { get; init; }
        public uint SoundEntriesId { get; init; }
        public uint EmotesId { get; init; }
        public uint Flags { get; init; }
    }
}