using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionView.xaml
    /// </summary>
    public class SmartActionView : SelectableTemplatedControl
    {
        public static AvaloniaProperty DeselectAllButActionsRequestProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DeselectAllButActionsRequest));

        public static AvaloniaProperty EditActionCommandProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(EditActionCommand));
        
        public static AvaloniaProperty DirectEditParameterProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DirectEditParameter));

        public static AvaloniaProperty DirectOpenParameterProperty =
            AvaloniaProperty.Register<SmartActionView, ICommand>(nameof(DirectOpenParameter));

        private int indent;
        public static readonly DirectProperty<SmartActionView, int> IndentProperty 
            = AvaloniaProperty.RegisterDirect<SmartActionView, int>("Indent", o => o.Indent, (o, v) => o.Indent = v);

        public ICommand DeselectAllButActionsRequest
        {
            get => (ICommand?) GetValue(DeselectAllButActionsRequestProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(DeselectAllButActionsRequestProperty, value);
        }

        public ICommand EditActionCommand
        {
            get => (ICommand?) GetValue(EditActionCommandProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(EditActionCommandProperty, value);
        }
        
        public ICommand DirectEditParameter
        {
            get => (ICommand?) GetValue(DirectEditParameterProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(DirectEditParameterProperty, value);
        }

        public ICommand DirectOpenParameter
        {
            get => (ICommand?) GetValue(DirectOpenParameterProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(DirectOpenParameterProperty, value);
        }

        public int Indent
        {
            get { return indent; }
            set { SetAndRaise(IndentProperty, ref indent, value); }
        }
        
        protected override void DeselectOthers()
        {
            DeselectAllButActionsRequest?.Execute(null);
        }

        protected override void OnDirectEdit(bool controlPressed, object context)
        {
            if (controlPressed)
                DirectOpenParameter?.Execute(context);
            else
                DirectEditParameter?.Execute(context);
        }

        protected override void OnEdit()
        {
            EditActionCommand?.Execute(DataContext);
        }

        protected override void OnDataContextEndUpdate()
        {
            var isComment = DataContext is SmartAction action && action.Id == SmartConstants.ActionComment;
            PseudoClasses.Set(":comment", isComment);
            PseudoClasses.Set(":action", !isComment);
        }
    }
}