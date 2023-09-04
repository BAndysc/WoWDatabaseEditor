using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.SmartScriptEditor.Models;
using PropertyChanged.SourceGenerator;
using WDE.Common.Disposables;
using WDE.Common.Utils;
using WDE.MVVM.Observable;

namespace WDE.SmartScriptEditor.Editor.ViewModels;

[AutoRegister]
public partial class VariablePickerViewModel : ObservableBase, IDialog
{
    private readonly GlobalVariableType type;
    private readonly SmartScriptBase script;

    public VariablePickerViewModel(GlobalVariableType type, SmartScriptBase script, long? initial)
    {
        this.type = type;
        this.script = script;
        Items = new(script.GlobalVariables.Where(gv => gv.VariableType == type)
            .OrderBy(i => i.Key)
            .Distinct(Compare.By<GlobalVariable, long>(i => i!.Key))
            .Select(gv => new VariableItemViewModel(gv)));

        SelectedItem = initial.HasValue ? Items.FirstOrDefault(i => i.Id == initial) : null;

        if (SelectedItem == null && initial != null)
        {
            var phantom = VariableItemViewModel.CreatePhantom(initial.Value);
            Items.Add(phantom);
            SelectedItem = phantom;
        }

        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
        Accept = new DelegateCommand(() =>
        {
            var dict = Items.Where(i => !i.IsPhantom).SafeToDictionary(i => i.OriginalId ?? i.Id, i => i);
            for (var i = script.GlobalVariables.Count - 1; i >= 0; i--)
            {
                var existing = script.GlobalVariables[i];
                if (existing.VariableType == type)
                {
                    if (dict.TryGetValue(existing.Key, out var item))
                    {
                        existing.Key = item.Id;
                        existing.Name = item.Name;
                        existing.Comment = item.Comment;
                        existing.Entry = item.Entry;
                        dict.Remove(item.OriginalId ?? item.Id);
                    }
                    else
                        script.GlobalVariables.RemoveAt(i);
                }
            }

            foreach (var pair in dict)
            {
                script.GlobalVariables.Add(new GlobalVariable()
                {
                    Key = pair.Key,
                    Name = pair.Value.Name,
                    Comment = pair.Value.Comment,
                    VariableType = type,
                    Entry = pair.Value.Entry
                });
            }

            CloseOk?.Invoke();
        });

        AddItemCommand = new DelegateCommand(() =>
        {
            var phantom = VariableItemViewModel.CreatePhantom((Items.Count > 0 ? Items.Max(i => i.Id) : 0) + 1);
            Items.Add(phantom);
            SelectedItem = phantom;
        });

        RemoveItemCommand = new DelegateCommand(() =>
        {
            if (SelectedItem != null)
            {
                Items.Remove(SelectedItem);
                SelectedItem = null;
            }
        }, () => SelectedItem != null).ObservesProperty(() => SelectedItem);

        PickVariable = new DelegateCommand<VariableItemViewModel>(item =>
        {
            SelectedItem = item;
            Accept.Execute(null);
        });
        
        Items.DisposeOnRemove();
        AutoDispose(new ActionDisposable(() => Items.RemoveAll()));
    }

    [Notify] private VariableItemViewModel? selectedItem;
    public ObservableCollection<VariableItemViewModel> Items { get; }

    public DelegateCommand AddItemCommand { get; }
    public DelegateCommand RemoveItemCommand { get; }
    public DelegateCommand<VariableItemViewModel> PickVariable { get; }
    
    public int DesiredWidth => 700;
    public int DesiredHeight => 500;
    public string Title => "Pick a variable";
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
}

public partial class VariableItemViewModel : ObservableBase
{
    [Notify] private long id;
    [Notify] private uint entry;
    [Notify] private string name;
    [Notify] private string? comment;
    [Notify] private bool isPhantom;
    public readonly long? OriginalId;

    private VariableItemViewModel(long id, string name)
    {
        this.id = id;
        this.name = name;
        this.comment = null;
        isPhantom = true;
        AutoDispose(this.ToObservable(o => o.Name)
            .Skip(1)
            .SubscribeAction(_ => IsPhantom = false));
    }
    
    public VariableItemViewModel(GlobalVariable variable)
    {
        id = variable.Key;
        name = variable.Name;
        comment = variable.Comment;
        entry = variable.Entry;
        OriginalId = id;
    }

    public static VariableItemViewModel CreatePhantom(long id)
    {
        return new VariableItemViewModel(id, "(unnamed)");
    }
}

public interface IVariablePickerService
{
    Task<long?> PickVariable(GlobalVariableType type, SmartScriptBase script, long? currentValue);
}

[AutoRegister]
[SingleInstance]
public class VariablePickerService : IVariablePickerService
{
    private readonly IWindowManager windowManager;
    private readonly IContainerProvider containerProvider;

    public VariablePickerService(IWindowManager windowManager, 
        IContainerProvider containerProvider)
    {
        this.windowManager = windowManager;
        this.containerProvider = containerProvider;
    }
    
    public async Task<long?> PickVariable(GlobalVariableType type, SmartScriptBase script, long? currentValue)
    {
        using var vm = containerProvider.Resolve<VariablePickerViewModel>((typeof(GlobalVariableType), type),
            (typeof(SmartScriptBase), script), (typeof(long?), currentValue));

        if (await windowManager.ShowDialog(vm))
        {
            return vm.SelectedItem?.Id;
        }

        return null;
    }
}