using System;
using System.Windows.Input;
using DynamicData.Binding;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Utils.DragDrop;
using WDE.DatabaseEditors.Data.Structs;
using WDE.MVVM;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;

public partial class ForeignTableViewModel : ObservableBase, IDropTarget
{
    public DefinitionViewModel Parent { get; }
    [Notify] private string tableName;
    [Notify] private string? autofillBuildColumn;

    [Notify] private ForeignKeyViewModel? selectedForeignKey;
    public ObservableCollectionExtended<ForeignKeyViewModel> ForeignKeys { get; } =
        new ObservableCollectionExtended<ForeignKeyViewModel>();
    
    public ICommand AddForeignKey { get; }
    public ICommand DeleteSelectedForeignKey { get; }
    
    public partial class ForeignKeyViewModel : ObservableBase
    {
        public ForeignTableViewModel Parent { get; }
        [Notify] private string columnName;
        
        public ForeignKeyViewModel(ForeignTableViewModel parent, string columnName)
        {
            Parent = parent;
            this.columnName = columnName;
        }
    }
    
    public ForeignTableViewModel(DefinitionViewModel parent, DatabaseForeignTableJson json)
    {
        Parent = parent;
        tableName = json.TableName;
        autofillBuildColumn = json.AutofillBuildColumn;
        foreach (var key in (json.ForeignKeys ?? []))
        {
            if (key.ForeignTable != null)
                throw new Exception("Foreign table in foreign key does not make sense");
            ForeignKeys.Add(new ForeignKeyViewModel(this, key.ColumnName));
        }
        if (ForeignKeys.Count > 0)
            SelectedForeignKey = ForeignKeys[0];
        
        AddForeignKey = new DelegateCommand(() =>
        {
            ForeignKeys.Add(new ForeignKeyViewModel(this, "-"));
            SelectedForeignKey = ForeignKeys[^1];
        });
        DeleteSelectedForeignKey = new DelegateCommand(() =>
        {
            if (SelectedForeignKey != null)
                ForeignKeys.Remove(SelectedForeignKey);
        });
        On(() => TableName, _ =>
        {
            // a little hack so that the parent is notified about the table name change
            // without observing on each individual item
            // (it works, because it already observes for the collection change, but not for the property change)
            if (Parent.ForeignTables.Count > 0)
            {
                var selected = Parent.SelectedForeignTable;
                Parent.ForeignTables.Move(0, 0);
                Parent.SelectedForeignTable = selected;
            }
        });
    }
    
    public void DragOver(IDropInfo dropInfo)
    {
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        dropInfo.Effects = DragDropEffects.Move;
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not ForeignKeyViewModel data)
            return;

        int indexOf = ForeignKeys.IndexOf(data);
        int dropIndex = dropInfo.InsertIndex;
        if (indexOf < dropIndex)
            dropIndex--;

        ForeignKeys.RemoveAt(indexOf);
        ForeignKeys.Insert(dropIndex, data);
        SelectedForeignKey = data;
    }
}