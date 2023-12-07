using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class IndexColumnsEditorDialogViewModel : ObservableBase, IDialog
{
    private readonly IReadOnlyList<ColumnViewModel> columns;
    private readonly IndexViewModel index;
    public int DesiredWidth => 500;
    public int DesiredHeight => 450;
    public string Title => "Index columns";
    public bool Resizeable => true;
    public ICommand Accept { get; }
    public ICommand Cancel { get; }
    public event Action? CloseCancel;
    public event Action? CloseOk;

    [Notify] private IndexPartViewModel? selectedPart;
    
    public ObservableCollectionExtended<IndexPartViewModel> Parts { get; } = new();
    
    public IReadOnlyList<ColumnViewModel> Columns => columns;
    
    public ICommand AddColumnCommand { get; }
    
    public ICommand RemoveSelectedCommand { get; }
    
    public IndexColumnsEditorDialogViewModel(IReadOnlyList<ColumnViewModel> columns,
        IndexViewModel index)
    {
        this.columns = columns;
        this.index = index;
        
        Parts.AddRange(index.Parts.Select(x => x.Clone()));
        SelectedPart = Parts.FirstOrDefault();
        
        AddColumnCommand = new DelegateCommand(() =>
        {
            var indexOfSelected = Math.Clamp(SelectedPart == null ? Parts.Count : Parts.IndexOf(SelectedPart) + 1, 0, Parts.Count);
            Parts.Insert(indexOfSelected, new IndexPartViewModel()
            {
            });
            SelectedPart = Parts[indexOfSelected];
        });
        
        RemoveSelectedCommand = new DelegateCommand(() =>
        {
            if (SelectedPart != null)
                Parts.Remove(SelectedPart);
        }, () => SelectedPart != null).ObservesProperty(() => SelectedPart);
        
        Accept = new DelegateCommand(() =>
        {
            CloseOk?.Invoke();
        });
        
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
    }
}