using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AsyncAwaitBestPractices.MVVM;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.Conditions.Data;

namespace WDE.Conditions.ViewModels
{
    public class ConditionEditorViewModel: BindableBase, IDialog
    {
        private readonly IWindowManager windowManager;
        private readonly IParameterFactory parameterFactory;
        
        public ConditionEditorViewModel(in ConditionJsonData source, IWindowManager windowManager, IParameterFactory parameterFactory)
        {
            this.windowManager = windowManager;
            this.parameterFactory = parameterFactory;
            
            Source = new ConditionEditorData(in source);
            SelectedParamIndex = -1;
            Save = new DelegateCommand(() => CloseOk?.Invoke());
            DeleteParam = new DelegateCommand(DeleteItem);
            EditParam = new AsyncCommand<ConditionParameterJsonData?>(EditItem);
            AddParam = new DelegateCommand(AddItem);
        }
        
        public ConditionEditorData Source { get; }
        public int SelectedParamIndex { get; set; }
        
        public DelegateCommand Save { get; }
        public DelegateCommand DeleteParam { get; }
        public AsyncCommand<ConditionParameterJsonData?> EditParam { get; }
        public DelegateCommand AddParam { get; }

        private void DeleteItem()
        {
            if (SelectedParamIndex >= 0)
                Source.Parameters.RemoveAt(SelectedParamIndex);
        }
        
        private void AddItem()
        {
            OpenEditor(new ConditionParameterJsonData(), true);
        }

        private async Task EditItem(ConditionParameterJsonData? item)
        {
            if (item.HasValue)
                await OpenEditor(item.Value, false);
        }
        
        private async Task OpenEditor(ConditionParameterJsonData item, bool isCreating)
        {
            var vm = new ConditionsParameterEditorViewModel(in item, parameterFactory);
            if (await windowManager.ShowDialog(vm) && !vm.IsEmpty())
            {
                if (isCreating)
                    Source.Parameters.Add(vm.Source.ToConditionParameterJsonData());
                else
                {
                    if (SelectedParamIndex >= 0)
                        Source.Parameters[SelectedParamIndex] = vm.Source.ToConditionParameterJsonData();
                }
            }
        }
        
        public bool IsEmpty() => Source.IsEmpty();
        
        public int DesiredWidth { get; } = 473;
        public int DesiredHeight { get; } = 666;
        public string Title { get; } = "Condition Editor";
        public bool Resizeable { get; } = false;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }

    public class ConditionEditorData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameReadable { get; set; }
        public string Description { get; set; }
        public string Help { get; set; }
        public ObservableCollection<ConditionParameterJsonData> Parameters { get; set; }

        public ConditionEditorData(in ConditionJsonData source)
        {
            Id = source.Id;
            Name = source.Name;
            NameReadable = source.NameReadable;
            Description = source.Description;
            Help = source.Help;
            if (source.Parameters != null)
                Parameters = new ObservableCollection<ConditionParameterJsonData>(source.Parameters);
            else
                Parameters = new ObservableCollection<ConditionParameterJsonData>();
        }

        public ConditionJsonData ToConditionJsonData()
        {
            ConditionJsonData obj = new();
            obj.Id = Id;
            obj.Name = Name;
            obj.NameReadable = NameReadable;
            obj.Description = Description;
            obj.Help = Help;
            if (Parameters.Count > 0)
                obj.Parameters = Parameters.ToList();
            
            return obj;
        }
        
        public bool IsEmpty() => string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(NameReadable);
    }
}