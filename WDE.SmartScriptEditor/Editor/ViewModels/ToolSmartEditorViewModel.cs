using System.Windows;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public interface IToolSmartEditorViewModel
    {
        bool IsOpened { get; }
        void ShowEditor(SmartScript script, ParametersEditViewModel viewModel);
        SmartScript CurrentScript { get; }
    }
    
    public class ToolSmartEditorViewModel : ObservableBase, ITool, IToolSmartEditorViewModel
    {
        public bool IsOpened => Visibility;
        public string Title => "Smart edit";
        public string UniqueId => "tc_smart_script_edit";
        public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Left;
        public bool OpenOnStart => false;
        private bool isSelected;
        private bool visibility;

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
        
        public bool Visibility
        {
            get => visibility;
            set => SetProperty(ref visibility, value);
        }
        
        public SmartScript CurrentScript { get; private set; }

        private ReactiveProperty<ParametersEditViewModel> editorViewModel = new(null);
        public ParametersEditViewModel EditorViewModel { get; private set; }

        public ToolSmartEditorViewModel()
        {
            Link(editorViewModel, () => EditorViewModel);
        }
        
        public void ShowEditor(SmartScript script, ParametersEditViewModel viewModel)
        {
            CurrentScript = script;
            editorViewModel.Value = viewModel;
        }
    }
}