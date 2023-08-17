using PropertyChanged.SourceGenerator;
using WDE.Common.CoreVersion;
using WDE.Common.Types;
using WDE.MVVM;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;

public partial class CoreVersionViewModel : ObservableBase
{
    [Notify] private bool isChecked;
    public string Tag { get; }
    public ImageUri Icon { get; }
    
    public CoreVersionViewModel(ICoreVersion coreVersion)
    {
        Icon = coreVersion.Icon;
        Tag = coreVersion.Tag;
    }

    public override string ToString()
    {
        return Tag;
    }
}