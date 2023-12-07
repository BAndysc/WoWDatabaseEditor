using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.ViewModels;

internal class EngineViewModel
{
    public EngineViewModel(string name, bool isDefaultPlaceholder)
    {
        Name = name;
        IsDefaultPlaceholder = isDefaultPlaceholder;
    }

    public EngineViewModel(TableEngine engine)
    {
        Name = engine.Name;
        IsDefault = engine.IsDefault;
        IsDefaultPlaceholder = false;
        Description = engine.Description;
        SupportsTransactions = engine.SupportsTransactions;
        SupportsXa = engine.SupportsXa;
        SupportsSavePoints = engine.SupportsSavePoints;
    }

    public bool? SupportsSavePoints { get; }
    public bool? SupportsXa { get; }
    public bool? SupportsTransactions { get; }
    public string? Description { get; }
    public string Name { get; }
    public bool IsDefault { get; }
    public bool IsDefaultPlaceholder { get; }
    public override string ToString() => Name;
}