using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Avalonia.Managers;

public class WindowViewModelDocumentWrapper : ObservableBase, IWindowViewModel
{
    private readonly IDocument document;

    public IDocument Document => document;

    public WindowViewModelDocumentWrapper(IDocument document)
    {
        this.document = document;

        Watch(this.document.ToObservable(x => x.Title), () => Title);
    }

    public int DesiredWidth => 900;
    public int DesiredHeight => 700;
    public string Title => document.Title;
    public bool Resizeable => true;
    public ImageUri? Icon => document.Icon;
}