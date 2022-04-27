namespace WDE.Common.DBC;

public interface IItem
{
    uint Entry { get; }
    string Name { get; }
    uint DisplayId { get; }
}