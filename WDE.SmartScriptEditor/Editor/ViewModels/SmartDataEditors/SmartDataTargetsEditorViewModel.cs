using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    class SmartDataTargetsEditorViewModel: BindableBase, ISmartDataEditorModel, IDialog
    {
        private readonly IParameterFactory parameterFactory;
        private readonly IWindowManager windowManager;
        
        public SmartDataTargetsEditorViewModel(IParameterFactory parameterFactory, IWindowManager windowManager, in SmartGenericJsonData source, bool insertOnSave)
        {
            this.parameterFactory = parameterFactory;
            this.windowManager = windowManager;
            Source = new SmartDataTargetsEditorData(in source);
            var sourceTypes = source.Types ?? new List<string>();
            TargetTypes = new List<SmartDataTargetTypeData>();
            foreach (var type in SmartTargetTypes.GetAllTypes())
                TargetTypes.Add(new SmartDataTargetTypeData(type, sourceTypes.Contains(type)));
            InsertOnSave = insertOnSave;
            SaveItem = new DelegateCommand(() => CloseOk?.Invoke());
            DeleteItem = new DelegateCommand(DeleteParam);
            AddParameter = new DelegateCommand(AddParam);
            EditParameter = new DelegateCommand<SmartParameterJsonData?>(EditParam);
            SelectedParamIndex = -1;
        }
        public SmartDataTargetsEditorData Source { get; }
        public List<SmartDataTargetTypeData> TargetTypes { get; }
        public int  SelectedParamIndex { get; set; }
        
        public DelegateCommand SaveItem { get; }
        public DelegateCommand DeleteItem { get; }
        public DelegateCommand AddParameter { get; }
        public DelegateCommand<SmartParameterJsonData?> EditParameter { get; }
        
        public bool InsertOnSave { get; private set; }
        public SmartGenericJsonData GetSource() => Source.ToSmartGenericJsonData(TargetTypes);
        public bool IsSourceEmpty() => Source.IsEmpty() || TargetTypes.Count(x => x.IsChecked) == 0;

        public void DeleteParam()
        {
            if (SelectedParamIndex >= 0)
                Source.Parameters.RemoveAt(SelectedParamIndex);
        }
        
        private void AddParam()
        {
            var temp = new SmartParameterJsonData();
            OpenParameterEditor(in temp, true);
        }

        private void EditParam(SmartParameterJsonData? param)
        {
            if (param.HasValue)
                OpenParameterEditor(param.Value, false);
        }

        private void OpenParameterEditor(in SmartParameterJsonData item, bool insertOnSave)
        {
            var vm = new SmartDataParameterEditorViewModel(parameterFactory, in item);
            if (windowManager.ShowDialog(vm) && !vm.Source.IsEmpty())
            {
                if (insertOnSave)
                    Source.Parameters.Add(vm.Source.ToSmartParameterJsonData());
                else
                {
                    var newItem = vm.Source.ToSmartParameterJsonData();
                    var index = Source.Parameters.IndexOf(item);
                    if (index != -1)
                        Source.Parameters[index] = newItem;
                }
            }
        }

        public int DesiredWidth { get; } = 473;
        public int DesiredHeight { get; } = 666;
        public string Title { get; } = "Target Editor";
        public bool Resizeable { get; } = false;
        public event Action CloseCancel;
        public event Action CloseOk;
    }

    public class SmartDataTargetTypeData
    {
        public string Name { get; }
        public bool IsChecked { get; set; }

        internal SmartDataTargetTypeData(string name, bool isChecked)
        {
            Name = name;
            IsChecked = isChecked;
        }
    }
    
    public class SmartDataTargetsEditorData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameReadable { get; set; }
        public string Help { get; set; }
        public string Description { get; set; }
        public ObservableCollection<SmartParameterJsonData> Parameters { get; set; }
        public ObservableCollection<string> Types { get; set; }

        internal SmartDataTargetsEditorData(in SmartGenericJsonData source)
        {
            Id = source.Id;
            Name = source.Name;
            if (string.IsNullOrEmpty(Name))
                Name = "SMART_TARGET_";
            NameReadable = source.NameReadable;
            Help = source.Help;
            Description = source.Description;
            if (source.Parameters != null)
                Parameters = new ObservableCollection<SmartParameterJsonData>(source.Parameters);
            else
                Parameters = new ObservableCollection<SmartParameterJsonData>();
            if (source.Types != null)
                Types = new ObservableCollection<string>(source.Types);
            else
                Types = new ObservableCollection<string>();
        }

        public SmartGenericJsonData ToSmartGenericJsonData(List<SmartDataTargetTypeData> targetTypes)
        {
            var obj = new SmartGenericJsonData();
            obj.Id = Id;
            obj.Name = Name;
            obj.NameReadable = NameReadable;
            obj.Help = Help;
            obj.Description = Description;
            if (Parameters.Count > 0)
                obj.Parameters = Parameters.ToList();
            obj.Types = targetTypes.Where(x => x.IsChecked).Select(x => x.Name).ToList();
            return obj;
        }

        public bool IsEmpty() => string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(NameReadable);
    }
}
