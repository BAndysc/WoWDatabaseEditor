using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Services.TablesTool;

[SingleInstance]
[AutoRegister]
public class TablesToolService : ObservableBase, ITablesToolService
{
    private readonly IDocumentManager documentManager;
    private TablesToolViewModel? tool;
    private bool visibility;

    public TablesToolService(IDocumentManager documentManager)
    {
        this.documentManager = documentManager;
        tool = documentManager.GetTool<TablesToolViewModel>();
        tool.ToObservable(t => t.Visibility)
            .SubscribeAction(@is =>
            {
                Visibility = @is;
            });
    }

    public bool Visibility
    {
        get => visibility;
        set => SetProperty(ref visibility, value);
    }

    public void Open()
    {
        if (!Visibility)
            documentManager.OpenTool<TablesToolViewModel>();
    }

    public void Close()
    {
        if (Visibility && tool != null)
            tool.Visibility = false;
    }
}