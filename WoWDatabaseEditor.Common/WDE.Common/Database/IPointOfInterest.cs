namespace WDE.Common.Database
{
    public interface IPointOfInterest
    {
        uint Id { get; }
        float PositionX { get; }
        float PositionY { get; }
        uint Icon { get; }
        uint Flags { get; }
        uint Importance { get; }
        string Name { get; }
        int? VerifiedBuild { get; }
    }
    
    public class AbstractPointOfInterest : IPointOfInterest
    {
        public uint Id { get; init; }
        public float PositionX { get; init; }
        public float PositionY { get; init; }
        public uint Icon { get; init; }
        public uint Flags { get; init; }
        public uint Importance { get; init; }
        public string Name { get; init; } = "";
        public int? VerifiedBuild { get; init; }
    }
}