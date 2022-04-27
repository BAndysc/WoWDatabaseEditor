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
}