namespace WDE.Common.Database
{
    public enum CreatureTextType
    {
        Say = 12,
        Whisper = 15,
        Yell = 14,
        Emote = 16,
        BossEmote = 41,
        BossWhisper = 42
    }
    
    public enum CreatureTextRange
    {
        Normal = 0,
        Area = 1,
        Zone = 2,
        Map = 3,
        World = 4
    }
    
    public interface ICreatureText
    {
        uint CreatureId { get; }
        byte GroupId { get; }
        byte Id { get; }
        string? Text { get; }
        CreatureTextType Type { get; }
        byte Language { get; }
        float Probability { get; }
        uint Emote { get; }
        uint Duration { get; }
        uint Sound { get; }
        uint BroadcastTextId { get; }
        CreatureTextRange TextRange { get; }
        string? Comment { get; }

        /// <summary>
        /// a special field used only in IQueryGenerator&lt;ICreatureText&gt;
        /// this is the comment outputted in the sql query.
        /// We might need a better way  for handling this type of comments
        /// </summary>
        // ReSharper disable once InconsistentNaming
        string? __comment => null;
    }
    
    public class AbstractCreatureText : ICreatureText
    {
        public uint CreatureId { get; set; }
        public byte GroupId { get; set; }
        public byte Id { get; set; }
        public string? Text { get; set; }
        public CreatureTextType Type { get; set; }
        public byte Language { get; set; }
        public float Probability { get; set; }
        public uint Emote { get; set; }
        public uint Duration { get; set; }
        public uint Sound { get; set; }
        public uint BroadcastTextId { get; set; }
        public CreatureTextRange TextRange { get; set; }
        public string? Comment { get; set; }
        public string? __comment { get; set; }
    }
}