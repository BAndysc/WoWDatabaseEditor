using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.MVVM;

namespace WDE.Parameters.ViewModels;

public class MultipleParametersPickerViewModel : ObservableBase, IDialog
{
    private readonly IParameter<long> source;
    public ObservableCollection<MultipleParametersPickerItemViewModel> PickedElements { get; } = new();
    
    public MultipleParametersPickerViewModel(IParameterPickerService parameterPickerService, IParameter<long> source, IReadOnlyList<long> currentValues)
    {
        this.source = source;
        Accept = new DelegateCommand(() => CloseOk?.Invoke());
        Cancel = new DelegateCommand(() => CloseCancel?.Invoke());

        foreach (var key in currentValues)
        {
            PickedElements.Add(new MultipleParametersPickerItemViewModel(source, key));
        }
        
        PickedElements.Add(new MultipleParametersPickerItemViewModel(this.source));
        
        DeleteItemCommand = new DelegateCommand<MultipleParametersPickerItemViewModel>(item =>
        {
            if (item.IsPhantom)
                return;
            
            PickedElements.Remove(item);
        });

        PickKeyCommand = new AsyncCommand<MultipleParametersPickerItemViewModel>(async item =>
        {
            var result = await parameterPickerService.PickParameter(source, item!.Key);
            if (result.ok)
                item.Key = result.value;
        });

        AddMultipleCommand = new AsyncCommand(async () =>
        {
            var keys = await parameterPickerService.PickMultiple(source);
            foreach (var key in keys)
            {
                var index = (PickedElements.Count > 0 && PickedElements[^1].IsPhantom) ? PickedElements.Count - 1 : PickedElements.Count;
                PickedElements.Insert(index, new MultipleParametersPickerItemViewModel(source, key));
            }
        });

        PickedElements.CollectionChanged += ElementsChanged;
        foreach (var item in PickedElements)
            item.PropertyChanged += OnItemChanged;
    }

    private void ElementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (MultipleParametersPickerItemViewModel @new in e.NewItems!)
            {
                @new.PropertyChanged += OnItemChanged;
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (MultipleParametersPickerItemViewModel old in e.OldItems!)
            {
                old.PropertyChanged -= OnItemChanged;
            }
        }
        else
            throw new NotImplementedException();
    }
 
    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (PickedElements.Any(x => x.IsPhantom))
            return;
        
        PickedElements.Add(new MultipleParametersPickerItemViewModel(source));
    }

    public DelegateCommand<MultipleParametersPickerItemViewModel> DeleteItemCommand { get; }
    
    public AsyncCommand<MultipleParametersPickerItemViewModel> PickKeyCommand { get; }
    
    public AsyncCommand AddMultipleCommand { get; }
    
    public IReadOnlyList<long> Result => PickedElements.Where(x => !x.IsPhantom).Select(e => e.Key).ToList();

    public int DesiredWidth => 600;
    public int DesiredHeight => 500;
    public string Title { get; } = "Pick parameters";
    public bool Resizeable => true;
    public ICommand Accept { get; }
    public ICommand Cancel { get; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
}

public partial class MultipleParametersPickerItemViewModel : ObservableBase
{
    private readonly IParameter<long> parameter;
    [Notify] [AlsoNotify(nameof(Name))] private long key;
    [Notify] private bool isPhantom;
    
    public string Name => parameter.ToString(key, ToStringOptions.WithoutNumber);
    
    public MultipleParametersPickerItemViewModel(IParameter<long> parameter, long key)
    {
        this.parameter = parameter;
        Key = key;
    }

    public MultipleParametersPickerItemViewModel(IParameter<long> parameter)
    {
        this.parameter = parameter;
        IsPhantom = true;

        this.ToObservable(x => x.Key).Skip(1).Subscribe(_ =>
        {
            IsPhantom = false;
        });
    }
}