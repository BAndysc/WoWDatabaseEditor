using WDE.Common.Types;
using WDE.MVVM;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;

public class IconViewModel : ObservableBase
{
    public ImageUri Icon { get; }
    public string Path { get; }
    
    public IconViewModel(ImageUri imageUri)
    {
        Icon = imageUri;
        Path = imageUri.Uri ?? "";
    }

    public override string ToString()
    {
        return Path;
    }
}