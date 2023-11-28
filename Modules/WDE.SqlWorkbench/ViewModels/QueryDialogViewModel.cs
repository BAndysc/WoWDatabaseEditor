using System;
using AsyncAwaitBestPractices.MVVM;
using AvaloniaEdit.Document;
using Prism.Commands;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.SqlWorkbench.ViewModels;

internal interface IQueryDialogService
{
    void ShowQueryDialog(string query);
}

[AutoRegister]
[SingleInstance]
internal class QueryDialogService : IQueryDialogService
{
    private readonly IContainerProvider containerProvider;
    private readonly Lazy<IWindowManager> windowManager;

    public QueryDialogService(IContainerProvider containerProvider,
        Lazy<IWindowManager> windowManager)
    {
        this.containerProvider = containerProvider;
        this.windowManager = windowManager;
    }

    public void ShowQueryDialog(string query)
    {
        windowManager.Value.ShowWindow(containerProvider.Resolve<QueryDialogViewModel>((typeof(string), query)), out _);
    }
}

internal class QueryDialogViewModel : ObservableBase, IWindowViewModel
{
    public int DesiredWidth => 800;
    public int DesiredHeight => 600;
    public string Title => "Query";
    public bool Resizeable => true;
    public ImageUri? Icon { get; } = new ImageUri("Icons/icon_to_sql.png");

    public DelegateCommand CopyCommand { get; }
    
    public TextDocument Document { get; } = new();
    
    public QueryDialogViewModel(IClipboardService clipboardService, 
        string query)
    {
        CopyCommand = new DelegateCommand(() => clipboardService.SetText(query));
        Document.Text = query;
    }
}