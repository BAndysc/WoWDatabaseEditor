using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.QuickAccess;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.QuickAccess;

[AutoRegister]
[SingleInstance]
public class QuickAccessViewModel : ObservableBase, IQuickAccessViewModel
{
    private readonly IQuickAccessService quickAccessService;
    private readonly IMainThread mainThread;
    private string searchText = "";
    public ObservableCollection<QuickAccessItemViewModel> Items { get; } = new();

    public ICommand CloseQuickAccessCommand { get; }
    
    public string SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value);
    }

    public bool IsOpened
    {
        get => isOpened;
        set => SetProperty(ref isOpened, value);
    }

    private CancellationTokenSource? pendingSearch;
    
    private bool isOpened;
    
    public bool IsSearching => pendingSearch != null;

    public QuickAccessViewModel(IQuickAccessService quickAccessService, IMainThread mainThread)
    {
        this.quickAccessService = quickAccessService;
        this.mainThread = mainThread;
        AutoDispose(this
            .ToObservable(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(10))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(text =>
        {
            pendingSearch?.Cancel();
            pendingSearch = null;
            Items.Clear();
            if (string.IsNullOrEmpty(text))
                return;
            
            pendingSearch = new CancellationTokenSource();
            SearchAsync(text, pendingSearch).ListenErrors();
        }));

        CloseQuickAccessCommand = new DelegateCommand(CloseSearch);
    }
    
    private async Task SearchAsync(string text, CancellationTokenSource token)
    {
        await Task.Run(async () =>
        {
            await quickAccessService.Search(text, item =>
            {
                if (pendingSearch == token)
                    mainThread.Dispatch(() => Items.Add(new QuickAccessItemViewModel(item)));
            }, token.Token);
            if (token.IsCancellationRequested)
                return;
        });
        if (token.IsCancellationRequested)
            return;
        if (Items.Count < 20)
        {
            Items.Sort((x, y) => x!.Score.CompareTo(y!.Score));
        }

        if (token == pendingSearch)
            pendingSearch = null;
    }

    public void OpenSearch(string? text)
    {
        IsOpened = true;
        SearchText = text ?? "";
    }

    public void CloseSearch()
    {
        IsOpened = false;
        SearchText = "";
    }

    public void Commit(QuickAccessItemViewModel vm)
    {
        vm.Execute();
    }
}

public class QuickAccessItemViewModel
{
    private readonly QuickAccessItem item;

    public QuickAccessItemViewModel(QuickAccessItem item)
    {
        this.item = item;
    }

    public ImageUri Icon => item.Icon;

    public string ActionText => item.Action;

    public string BottomText => item.Description;

    public string MainText => item.Text;
    
    public byte Score => item.Score;

    public void Execute()
    {
        item.Command.Execute(item.Parameter);
    }
}