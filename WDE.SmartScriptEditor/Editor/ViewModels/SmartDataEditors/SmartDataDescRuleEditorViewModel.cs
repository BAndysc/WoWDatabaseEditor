using System;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Editor.ViewModels.SmartDataEditors
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
            AddCondition = new AsyncAutoCommand(AddCond);
            EditCondition = new AsyncAutoCommand<SmartConditionalJsonData?>(EditCond);
            SelectedCondIndex = -1;
        }
        
        public SmartDataDescRuleEditor Source { get; }
        public int SelectedCondIndex { get; set; }
        public bool InsertOnSave { get; }
        
        public DelegateCommand Save { get; }
        public DelegateCommand Delete { get; }
        public AsyncAutoCommand AddCondition { get; }
        public AsyncAutoCommand<SmartConditionalJsonData?> EditCondition { get; }

        public SmartDescriptionRulesJsonData GetSource() => Source.ToSmartDescriptionRulesJsonData();
        public bool IsSourceEmpty() => Source.IsEmpty();

        private async System.Threading.Tasks.Task EditCond(SmartConditionalJsonData? cond)
        {
            if (cond.HasValue)
                await OpenConditionEditor(cond.Value, false);
        }
        
        private async System.Threading.Tasks.Task AddCond()
        {
            await OpenConditionEditor( new SmartConditionalJsonData(), true);
        }

        private void DeleteCondition()
        {
            if (SelectedCondIndex >= 0)
                Source.Conditions.RemoveAt(SelectedCondIndex);
        }
        
        private async System.Threading.Tasks.Task OpenConditionEditor(SmartConditionalJsonData item, bool insertOnSave)
        {
            var vm = new SmartDataConditionEditorViewModel(in item, insertOnSave, true);
            if (await windowManager.ShowDialog(vm) && !vm.IsSourceEmpty())
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
        public event Action? CloseCancel;
        public event Action? CloseOk;
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