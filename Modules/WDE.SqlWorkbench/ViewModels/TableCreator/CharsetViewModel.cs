using System.Collections.ObjectModel;

namespace WDE.SqlWorkbench.ViewModels;

internal class CharsetViewModel
{
    public ObservableCollection<CollationViewModel> Collations { get; } = new();
    
    public CharsetViewModel(string name, bool isDefaultPlaceholder)
    {
        Name = name;
        IsDefaultPlaceholder = isDefaultPlaceholder;
    }

    public string Name { get; }
    public bool IsDefaultPlaceholder { get; }
    public override string ToString() => Name;
}