namespace WDE.SqlWorkbench.Models;

internal readonly struct TableEngine
{
    public readonly string Name;
    public readonly bool IsDefault;
    public readonly bool IsSupported;
    public readonly string? Description;
    public readonly bool? SupportsTransactions;
    public readonly bool? SupportsXa;
    public readonly bool? SupportsSavePoints;

    public TableEngine(string name, bool isDefault, bool isSupported, string? description, bool? supportsTransactions, bool? supportsXa, bool? supportsSavePoints)
    {
        Name = name;
        IsDefault = isDefault;
        IsSupported = isSupported;
        Description = description;
        SupportsTransactions = supportsTransactions;
        SupportsXa = supportsXa;
        SupportsSavePoints = supportsSavePoints;
    }
}