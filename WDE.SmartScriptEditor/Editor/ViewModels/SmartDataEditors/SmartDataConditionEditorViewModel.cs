using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Editor.ViewModels.SmartDataEditors
{
    public class SmartDataConditionEditorViewModel: BindableBase, IDialog
    {
        public SmartDataConditionEditorViewModel(in SmartConditionalJsonData item, bool insertOnSave, bool isExtendedEditor)
        {
            InsertOnSave = insertOnSave;
            CompareTypes = Enum.GetValues<CompareType>().ToList();
            WarningTypes = Enum.GetValues<WarningType>().ToList();
            IsExtendedEditor = isExtendedEditor;
            Source = new SmartDataConditionEditorData(in item, isExtendedEditor);
            Save = new DelegateCommand(() => CloseOk?.Invoke());
            selectedTypeIndex = CompareTypes.IndexOf(Source.CompareType);
            selectedWarningIndex = WarningTypes.IndexOf(Source.WarningType);
        }
        
        public SmartDataConditionEditorData Source { get; }
        public List<CompareType> CompareTypes { get; }
        public List<WarningType> WarningTypes { get; }
        public bool IsExtendedEditor { get; }

        private int selectedTypeIndex;
        public int SelectedTypeIndex
        {
            get => selectedTypeIndex;
            set
            {
                selectedTypeIndex = value;
                if (value >= 0)
                    Source.CompareType = CompareTypes[value];
            }
        }
        
        private int selectedWarningIndex = 0;
        public int SelectedWarningIndex
        {
            get => selectedWarningIndex;
            set
            {
                selectedWarningIndex = value;
                if (value >= 0)
                    Source.WarningType = WarningTypes[value];
            }
        }
        
        public DelegateCommand Save { get; }
        
        public int DesiredWidth { get; } = 473;
        public int DesiredHeight { get; } = 676;
        public string Title { get; } = "Condition Editor";
        public bool Resizeable { get; } = false;
        public event Action CloseCancel;
        public event Action CloseOk;
        public bool InsertOnSave { get; }
        public SmartConditionalJsonData GetSource() => Source.ToSmartConditionalJsonData();

        public bool IsSourceEmpty() => Source.IsEmpty();
    }

    public class SmartDataConditionEditorData
    {
        public string Type { get; }
        public WarningType WarningType { get; set; }
        public CompareType CompareType { get; set; }
        public bool Invert { get; set; }
        public int ComparedParam { get; set; }
        public int ComparedToParam { get; set; }
        public int CompareToValue { get; set; }
        public string ComparedAnyParam { get; set; }
        public string ComparedToAnyParam { get; set; }
        public string Error { get; set; }

        private readonly bool isExtendedModel;

        public SmartDataConditionEditorData(in SmartConditionalJsonData source, bool isExtended)
        {
            isExtendedModel = isExtended;
            Type = isExtendedModel ? "EQUALS" : "CompareValue";
            WarningType = source.WarningType;
            CompareType = source.CompareType;
            Invert = source.Invert;
            ComparedParam = source.ComparedParameterId;
            ComparedToParam = source.CompareToParameterId;
            CompareToValue = source.CompareToValue;
            ComparedAnyParam = source.ComparedAnyParam;
            ComparedToAnyParam = source.ComparedToAnyParam;
            Error = source.Error;
        }

        public SmartConditionalJsonData ToSmartConditionalJsonData()
        {
            SmartConditionalJsonData obj = new();
            obj.Type = Type;
            obj.WarningType = WarningType;
            obj.CompareType = CompareType;
            obj.Invert = Invert;
            obj.ComparedParameterId = ComparedParam;
            obj.CompareToParameterId = ComparedToParam;
            obj.CompareToValue = CompareToValue;
            obj.ComparedAnyParam = ComparedAnyParam;
            obj.ComparedToAnyParam = ComparedToAnyParam;
            obj.Error = Error;
            return obj;
        }

        public bool IsEmpty() => !isExtendedModel && (ComparedParam == 0 || ComparedToParam == 0);
    }
}