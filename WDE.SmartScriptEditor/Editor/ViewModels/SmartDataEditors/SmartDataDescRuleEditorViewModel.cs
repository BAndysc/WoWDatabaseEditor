using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Mvvm;
using Prism.Commands;
using WDE.SmartScriptEditor.Data;
using WDE.Common.Managers;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartDataDescRuleEditorViewModel: BindableBase, IDialog
    {
        private readonly IWindowManager windowManager;
        public SmartDataDescRuleEditorViewModel(IWindowManager windowManager, in SmartDescriptionRulesJsonData source, bool insertOnSave)
        {
            InsertOnSave = insertOnSave;
            this.windowManager = windowManager;
            Source = new SmartDataDescRuleEditor(in source);
            Save = new DelegateCommand(() => CloseOk?.Invoke());
            Delete = new DelegateCommand(DeleteCondition);
            AddCondition = new DelegateCommand(AddCond);
            EditCondition = new DelegateCommand<SmartConditionalJsonData?>(EditCond);
            SelectedCondIndex = -1;
        }
        
        public SmartDataDescRuleEditor Source { get; }
        public int SelectedCondIndex { get; set; }
        public bool InsertOnSave { get; }
        
        public DelegateCommand Save { get; }
        public DelegateCommand Delete { get; }
        public DelegateCommand AddCondition { get; }
        public DelegateCommand<SmartConditionalJsonData?> EditCondition { get; }

        public SmartDescriptionRulesJsonData GetSource() => Source.ToSmartDescriptionRulesJsonData();
        public bool IsSourceEmpty() => Source.IsEmpty();

        private void EditCond(SmartConditionalJsonData? cond)
        {
            if (cond.HasValue)
                OpenConditionEditor(cond.Value, false);
        }
        
        private void AddCond()
        {
            OpenConditionEditor( new SmartConditionalJsonData(), true);
        }

        private void DeleteCondition()
        {
            if (SelectedCondIndex >= 0)
                Source.Conditions.RemoveAt(SelectedCondIndex);
        }
        
        private void OpenConditionEditor(SmartConditionalJsonData item, bool insertOnSave)
        {
            var vm = new SmartDataConditionEditorViewModel(in item, insertOnSave, true);
            if (windowManager.ShowDialog(vm) && !vm.IsSourceEmpty())
            {
                if (vm.InsertOnSave)
                    Source.Conditions.Add(vm.GetSource());
                else
                {
                    var newItem = vm.GetSource();
                    var index = Source.Conditions.IndexOf(item);
                    if (index != -1)
                        Source.Conditions[index] = newItem;
                }
            }
        }
        
        public int DesiredWidth { get; } = 473;
        public int DesiredHeight { get; } = 676;
        public string Title { get; } = "Description Rule Editor";
        public bool Resizeable { get; } = false;
        public event Action CloseCancel;
        public event Action CloseOk;
    }

    public class SmartDataDescRuleEditor
    {
        public string Description { get; set; }
        public ObservableCollection<SmartConditionalJsonData> Conditions { get; set; }
        public SmartDataDescRuleEditor(in SmartDescriptionRulesJsonData source)
        {
            Description = source.Description;
            if (source.Conditions != null)
                Conditions = new ObservableCollection<SmartConditionalJsonData>(source.Conditions);
            else
                Conditions = new ObservableCollection<SmartConditionalJsonData>();

        }

        public SmartDescriptionRulesJsonData ToSmartDescriptionRulesJsonData()
        {
            SmartDescriptionRulesJsonData obj = new();
            obj.Description = Description;
            if (Conditions.Count > 0)
                obj.Conditions = Conditions.ToList();
            return obj;
        }

        public bool IsEmpty() => string.IsNullOrWhiteSpace(Description) || Conditions.Count == 0;
    }
}