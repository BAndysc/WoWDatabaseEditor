namespace WDE.SqlWorkbench.ViewModels;

internal class CollationViewModel
{
    public CollationViewModel(string name, bool isDefaultPlaceholder, bool isDefault)
    {
        Name = name;
        IsDefault = isDefault;
        IsDefaultPlaceholder = isDefaultPlaceholder;
    }

    public string Name { get; }
    public bool IsDefault { get; }
    public bool IsDefaultPlaceholder { get; }
    public override string ToString() => Name;
}