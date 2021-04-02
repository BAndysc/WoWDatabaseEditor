using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Editor.ViewModels.SmartDataEditors
{
    public class SmartDataEventsEditorViewModel: BindableBase, ISmartDataEditorModel, IDialog
    {
        private readonly IParameterFactory parameterFactory;
        private readonly IWindowManager windowManager;
        
        public SmartDataEventsEditorViewModel(IParameterFactory parameterFactory, IWindowManager windowManager, in SmartGenericJsonData source, bool insertOnSave)
        {
            this.parameterFactory = parameterFactory;
            this.windowManager = windowManager;
            InsertOnSave = insertOnSave;
            Source = new SmartDataEventsEditorData(in source);
            MakeTypeItems();
            SaveItem = new DelegateCommand(() =>
            {
                Source.ValidTypes = SmartScriptTypes.Where(x => x.IsChecked).Select(x => x.Type).ToList();
                CloseOk?.Invoke();
            });
            EditParameter = new AsyncAutoCommand<SmartParameterJsonData?>(EditParam);
            AddParameter = new AsyncAutoCommand(AddParam);
            EditCondition = new AsyncAutoCommand<SmartConditionalJsonData?>(EditCond);
            AddCondition = new AsyncAutoCommand(AddCond);
            EditDescriptionDefinition = new AsyncAutoCommand<SmartDescriptionRulesJsonData?>(EditDescDefinition);
            AddDescriptionDefinition = new AsyncAutoCommand(AddDescDef);
            DeleteActiveItem = new DelegateCommand(DeleteItem);
            SelectedParamIndex = -1;
            SelectedCondIndex = -1;
            SelectedDescRuleIndex = -1;
        }
        
        public SmartDataEventsEditorData Source { get; }
        public int SelectedTab { get; set; }
        public int SelectedParamIndex { get; set; }
        public int SelectedCondIndex { get; set; }
        public int SelectedDescRuleIndex { get; set; }
        public List<SmartDataEventSmartTypeData> SmartScriptTypes { get; private set; }

        public bool InsertOnSave { get; private set; }
        public SmartGenericJsonData GetSource() => Source.ToSmartGenericJsonData();
        public bool IsSourceEmpty() => Source.IsEmpty();

        public DelegateCommand SaveItem { get; }
        public AsyncAutoCommand<SmartParameterJsonData?> EditParameter { get; }
        public AsyncAutoCommand AddParameter { get; }
        public AsyncAutoCommand<SmartConditionalJsonData?> EditCondition { get; }
        public AsyncAutoCommand AddCondition { get; }
        public AsyncAutoCommand<SmartDescriptionRulesJsonData?> EditDescriptionDefinition { get; }
        public AsyncAutoCommand AddDescriptionDefinition { get; }
        public DelegateCommand DeleteActiveItem { get; }

        private async Task EditParam(SmartParameterJsonData? param)
        {
            if (param.HasValue)
                await OpenParameterEditor(param.Value, false);
        }

        private async Task AddParam()
        {
            var temp = new SmartParameterJsonData();
            await OpenParameterEditor(temp, true);
        }

        private async Task EditCond(SmartConditionalJsonData? cond)
        {
            if (cond.HasValue)
                await OpenConditionEditor(cond.Value, false);
        }

        private async Task AddCond()
        {
            var temp = new SmartConditionalJsonData();
            await OpenConditionEditor(temp, true);
        }

        private async Task EditDescDefinition(SmartDescriptionRulesJsonData? descRule)
        {
            if (descRule.HasValue)
                await OpenDescriptionRuleEditor(descRule.Value, false);
        }

        private async Task AddDescDef()
        {
            var temp = new SmartDescriptionRulesJsonData();
            await OpenDescriptionRuleEditor(temp, true);
        }

        private void DeleteItem()
        {
            switch (SelectedTab)
            {
                case 0:
                    if (SelectedParamIndex >= 0)
                        Source.Parameters.RemoveAt(SelectedParamIndex);
                    break;
                case 1:
                    if (SelectedCondIndex >= 0)
                        Source.Conditions.RemoveAt(SelectedCondIndex);
                    break;
                case 2:
                    if (SelectedDescRuleIndex >= 0)
                        Source.DescriptionRules.RemoveAt(SelectedDescRuleIndex);
                    break;
            }
        }
        
        private async Task OpenParameterEditor(SmartParameterJsonData item, bool insertOnSave)
        {
            var vm = new SmartDataParameterEditorViewModel(parameterFactory, in item);
            if (await windowManager.ShowDialog(vm) && !vm.Source.IsEmpty())
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

        private async Task OpenConditionEditor(SmartConditionalJsonData item, bool insertOnSave)
        {
            var vm = new SmartDataConditionEditorViewModel(in item, insertOnSave, false);
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

        private async Task OpenDescriptionRuleEditor(SmartDescriptionRulesJsonData item, bool insertOnSave)
        {
            var vm = new SmartDataDescRuleEditorViewModel(windowManager, in item, insertOnSave);
            if (await windowManager.ShowDialog(vm) && !vm.IsSourceEmpty())
            {
                if (vm.InsertOnSave)
                    Source.DescriptionRules.Add(vm.GetSource());
                else
                {
                    var newItem = vm.GetSource();
                    var index = Source.DescriptionRules.IndexOf(item);
                    if (index != -1)
                        Source.DescriptionRules[index] = newItem;
                }
            }
        }
        
        private void MakeTypeItems()
        {
            var enumValues = System.Enum.GetValues<SmartScriptType>();
            SmartScriptTypes = new List<SmartDataEventSmartTypeData>(capacity: enumValues.Length);
            foreach (var enumVar in enumValues)
            {
                var isSelected = Source.ValidTypes.Contains(enumVar);
                SmartScriptTypes.Add(new SmartDataEventSmartTypeData(enumVar, isSelected));
            }
        }

        public int DesiredWidth { get; } = 473;
        public int DesiredHeight { get; } = 676;
        public string Title { get; } = "Event Editor";
        public bool Resizeable { get; } = false;
        public event Action CloseCancel;
        public event Action CloseOk;
    }

    public class SmartDataEventSmartTypeData
    {
        public SmartScriptType Type { get; set; }
        public string TypeName { get; set; }
        public bool IsChecked { get; set; }

        public SmartDataEventSmartTypeData(SmartScriptType type, bool isChecked)
        {
            Type = type;
            TypeName = type.ToString();
            IsChecked = isChecked;
        }
    }
    
    public class SmartDataEventsEditorData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ReadableName { get; set; }
        public string Description { get; set; }
        public bool IsTimed { get; set; }
        
        public List<SmartScriptType> ValidTypes { get; set; }
        public ObservableCollection<SmartParameterJsonData> Parameters { get; set; }
        public ObservableCollection<SmartConditionalJsonData> Conditions { get; set; }
        public ObservableCollection<SmartDescriptionRulesJsonData> DescriptionRules { get; set; }
        
        public SmartDataEventsEditorData(in SmartGenericJsonData source)
        {
            Id = source.Id;
            Name = source.Name;
            if (string.IsNullOrWhiteSpace(Name))
                Name = "SMART_EVENT_";
            ReadableName = source.NameReadable;
            Description = source.Description;
            IsTimed = source.IsTimed;
            if (source.UsableWithScriptTypes != null)
                ValidTypes = new List<SmartScriptType>(source.UsableWithScriptTypes);
            else
                ValidTypes = new List<SmartScriptType>();
            if (source.Parameters != null)
                Parameters = new ObservableCollection<SmartParameterJsonData>(source.Parameters);
            else
                Parameters = new ObservableCollection<SmartParameterJsonData>();
            if (source.Conditions != null)
                Conditions = new ObservableCollection<SmartConditionalJsonData>(source.Conditions);
            else
                Conditions = new ObservableCollection<SmartConditionalJsonData>();
            if (source.DescriptionRules != null)
                DescriptionRules = new ObservableCollection<SmartDescriptionRulesJsonData>(source.DescriptionRules);
            else
                DescriptionRules = new ObservableCollection<SmartDescriptionRulesJsonData>();
        }

        public SmartGenericJsonData ToSmartGenericJsonData()
        {
            SmartGenericJsonData obj = new();
            obj.Id = Id;
            obj.Name = Name;
            obj.NameReadable = ReadableName;
            obj.Description = Description;
            if (ValidTypes.Count > 0)
                obj.UsableWithScriptTypes = ValidTypes.ToList();
            if (Parameters.Count > 0)
                obj.Parameters = Parameters.ToList();
            if (Conditions.Count > 0)
                obj.Conditions = Conditions.ToList();
            if (DescriptionRules.Count > 0)
                obj.DescriptionRules = DescriptionRules.ToList();
            return obj;
        }

        public bool IsEmpty() => string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(ReadableName);
    }
}
