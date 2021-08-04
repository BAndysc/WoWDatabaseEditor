using System;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;
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
            Save = new DelegateCommand(() => CloseOk?.Invoke());
            SelectedCondIndex = -1;
        }
        public int SelectedCondIndex { get; set; }
        public bool InsertOnSave { get; }
        
        public DelegateCommand Save { get; }

        public int DesiredWidth { get; } = 473;
        public int DesiredHeight { get; } = 676;
        public string Title { get; } = "Description Rule Editor";
        public bool Resizeable { get; } = false;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }
}