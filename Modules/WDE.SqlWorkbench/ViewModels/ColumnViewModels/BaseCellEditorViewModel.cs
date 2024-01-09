using System.Windows.Input;
using Prism.Commands;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.ViewModels;

internal abstract class BaseCellEditorViewModel : ObservableBase
{
    protected readonly int rowIndex;
    protected bool isModified;
    public bool CanBeNull { get; }
    
    public ICommand ApplyChangesCommand { get; }

    public bool IsReadOnly { get; }
    
    private bool isNull;
    public bool IsNull
    {
        get => isNull;
        set
        {
            if (isNull == value)
                return;

            isModified = true;
            SetProperty(ref isNull, value);
            ApplyChanges();
        }
    }

    public string? Type { get; }

    public BaseCellEditorViewModel(string? mySqlType, ISparseColumnData overrideData, IColumnData columnData, int rowIndex, bool nullable, bool readOnly)
    {
        this.rowIndex = rowIndex;
        IsReadOnly = readOnly;
        Type = mySqlType;
        CanBeNull = nullable;
        ApplyChangesCommand = new DelegateCommand(ApplyChanges);
        // don't use the property here, because it will call ApplyChanges which is an abstract method and will not work in the base constructor!
        isNull = overrideData.HasRow(rowIndex) ? overrideData.IsNull(rowIndex) : columnData.IsNull(rowIndex);
    }

    public abstract void ApplyChanges();
}