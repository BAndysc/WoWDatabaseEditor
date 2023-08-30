using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Avalonia.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for EventAiActionView.xaml
    /// </summary>
    public class EventAiActionView : SelectableTemplatedControl
    {
        public static AvaloniaProperty DeselectAllButActionsRequestProperty =
            AvaloniaProperty.Register<EventAiActionView, ICommand>(nameof(DeselectAllButActionsRequest));

        public static AvaloniaProperty EditActionCommandProperty =
            AvaloniaProperty.Register<EventAiActionView, ICommand>(nameof(EditActionCommand));
        
        public static AvaloniaProperty DirectEditParameterProperty =
            AvaloniaProperty.Register<EventAiActionView, ICommand>(nameof(DirectEditParameter));

        private int indent;
        public static readonly DirectProperty<EventAiActionView, int> IndentProperty 
            = AvaloniaProperty.RegisterDirect<EventAiActionView, int>("Indent", o => o.Indent, (o, v) => o.Indent = v);

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

        public int Indent
        {
            get { return indent; }
            set { SetAndRaise(IndentProperty, ref indent, value); }
        }
        
        protected override void DeselectOthers()
        {
            DeselectAllButActionsRequest?.Execute(null);
        }

        protected override void OnDirectEdit(object context)
        {
            DirectEditParameter?.Execute(context);
        }

        protected override void OnEdit()
        {
            EditActionCommand?.Execute(DataContext);
        }

        protected override void OnDataContextEndUpdate()
        {
            var isComment = DataContext is EventAiAction action && action.Id == EventAiConstants.ActionComment;
            PseudoClasses.Set(":comment", isComment);
            PseudoClasses.Set(":action", !isComment);
        }
    }
}