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
        public uint CreatureId { get; }
        public byte GroupId { get; }
        public byte Id { get; }
        public string? Text { get; }
        public CreatureTextType Type { get; }
        public byte Language { get; }
        public float Probability { get; }
        public uint Emote { get; }
        public uint Duration { get; }
        public uint Sound { get; }
        public uint BroadcastTextId { get; }
        public CreatureTextRange TextRange { get; }
        public string? Comment { get; }
    }
}