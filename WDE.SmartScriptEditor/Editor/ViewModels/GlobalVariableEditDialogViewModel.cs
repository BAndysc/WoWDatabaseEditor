using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.MVVM;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class GlobalVariableEditDialogViewModel : ObservableBase, IDialog
    {
        private readonly GlobalVariable toEdit;

        private long key;
        public long Key
        {
            get => key;
            set => SetProperty(ref key, value);
        }

        private static ObservableCollection<GlobalVariableType> variableTypes = new();

        public ObservableCollection<GlobalVariableType> VariableTypes => variableTypes;
        
        private GlobalVariableType variableType;
        public GlobalVariableType VariableType
        {
            get => variableType;
            set => SetProperty(ref variableType, value);
        }
        
        private string name = "";
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private uint entry;
        public uint Entry
        {
            get => entry;
            set => SetProperty(ref entry, value);
        }

        private string? comment;
        public string? Comment
        {
            get => comment;
            set => SetProperty(ref comment, value);
        }

        static GlobalVariableEditDialogViewModel()
        {
            foreach (var e in Enum.GetValues<GlobalVariableType>())
                variableTypes.Add(e);
        }
        
        public GlobalVariableEditDialogViewModel(GlobalVariable toEdit, SmartScriptBase? script = null)
        {
            this.toEdit = toEdit;
            key = toEdit.Key;
            variableType = toEdit.VariableType;
            name = toEdit.Name;
            comment = toEdit.Comment;
            entry = toEdit.Entry;
            
            Accept = new DelegateCommand(() =>
            {
                CloseOk?.Invoke();
                using var _ = script?.BulkEdit("Edit global variable");
                toEdit.Key = Key;
                toEdit.VariableType = variableType;
                toEdit.Name = Name;
                toEdit.Comment = Comment;
                toEdit.Entry = Entry;
            });
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
        }
        
        public int DesiredWidth => 300;
        public int DesiredHeight => 400;
        public string Title => "Global variable edit";
        public bool Resizeable => false;
        public event Action? CloseCancel;
        public event Action? CloseOk;
        
        public ICommand Accept { get; set; }
        public ICommand Cancel { get; set; }
    }
}