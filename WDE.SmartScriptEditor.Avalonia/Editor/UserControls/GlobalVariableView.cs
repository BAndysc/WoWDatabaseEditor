using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Avalonia.Editor.Views;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionView.xaml
    /// </summary>
    public class GlobalVariableView : SelectableTemplatedControl
    {
        public static AvaloniaProperty DeselectAllButGlobalVariablesRequestProperty =
            AvaloniaProperty.Register<GlobalVariableView, ICommand>(nameof(DeselectAllButGlobalVariablesRequest));

        private SmartScriptBase? script;
        public static readonly DirectProperty<GlobalVariableView, SmartScriptBase?> ScriptProperty = AvaloniaProperty.RegisterDirect<GlobalVariableView, SmartScriptBase?>("Script", o => o.Script, (o, v) => o.Script = v);

        public ICommand DeselectAllButGlobalVariablesRequest
        {
            get => (ICommand?) GetValue(DeselectAllButGlobalVariablesRequestProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(DeselectAllButGlobalVariablesRequestProperty, value);
        }

        public SmartScriptBase? Script
        {
            get { return script; }
            set { SetAndRaise(ScriptProperty, ref script, value); }
        }

        protected override void DeselectOthers()
        {
            DeselectAllButGlobalVariablesRequest?.Execute(null);
        }

        protected override void OnEdit()
        {
            if (DataContext is GlobalVariable variable)
            {
                OpenEditFlyout(variable);
            }
        }

        private void OpenEditFlyout(GlobalVariable variable)
        {
            var flyout = new Flyout();
            var view = new GlobalVariableEditDialogView();
            var vm = new GlobalVariableEditDialogViewModel(variable, script);
            view.DataContext = vm;
            view.MinWidth = vm.DesiredWidth;
            flyout.Content = view;
            flyout.Placement = PlacementMode.BottomEdgeAlignedLeft;
            flyout.ShowAt(this);
        }
    }
}