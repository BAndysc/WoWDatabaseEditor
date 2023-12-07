using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.ViewModels;

internal class RowFormatViewModel
{
    public RowFormatViewModel(string name, bool isDefaultPlaceholder)
    {
        Name = name;
        IsDefaultPlaceholder = isDefaultPlaceholder;
    }
    
    public RowFormatViewModel(RowFormat format)
    {
        Name = format.ToString();
        Format = format;
        IsDefaultPlaceholder = false;
    }

    public string Name { get; }
    public RowFormat? Format { get; }
    public bool IsDefaultPlaceholder { get; }
    public override string ToString() => Name;
}