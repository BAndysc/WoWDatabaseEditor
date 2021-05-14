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
            var enumValues = System.Enum.GetValues<SmartScriptType>();
            SmartScriptTypes = new List<SmartDataEventSmartTypeData>(capacity: enumValues.Length);
            foreach (var enumVar in enumValues)
            {
                var isSelected = Source.ValidTypes.Contains(enumVar);
                SmartScriptTypes.Add(new SmartDataEventSmartTypeData(enumVar, isSelected));
            }
            SaveItem = new DelegateCommand(() =>
            {
                Source.ValidTypes = SmartScriptTypes.Where(x => x.IsChecked).Select(x => x.Type).ToList();
                CloseOk?.Invoke();
            });
            EditParameter = new AsyncAutoCommand<SmartParameterJsonData?>(EditParam);
            AddParameter = new AsyncAutoCommand(AddParam);
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

        private void DeleteItem()
        {
            switch (SelectedTab)
            {
                case 0:
                    if (SelectedParamIndex >= 0)
                        Source.Parameters.RemoveAt(SelectedParamIndex);
                    break;
                case 1:
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

        public int DesiredWidth { get; } = 473;
        public int DesiredHeight { get; } = 676;
        public string Title { get; } = "Event Editor";
        public bool Resizeable { get; } = false;
        public event Action? CloseCancel;
        public event Action? CloseOk;
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
            if (DescriptionRules.Count > 0)
                obj.DescriptionRules = DescriptionRules.ToList();
            return obj;
        }

        public bool IsEmpty() => string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(ReadableName);
    }
}
