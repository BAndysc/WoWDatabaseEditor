using System;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.DatabaseEditors.ViewModels;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.Services;

[SingleInstance]
[AutoRegister]
public class TablesToolService : ObservableBase, ITablesToolService
{
    private readonly Lazy<IDocumentManager> documentManager;
    private TablesListToolViewModel? tool;
    private bool visibility;

    public TablesToolService(IContainerProvider containerProvider, 
        IEventAggregator eventAggregator, Lazy<IDocumentManager> documentManager)
    {
        this.documentManager = documentManager;
        tool = documentManager.Value.GetTool<TablesListToolViewModel>();
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
            documentManager.Value.OpenTool<TablesListToolViewModel>();
    }

    public void Close()
    {
        if (Visibility && tool != null)
            tool.Visibility = false;
    }
}