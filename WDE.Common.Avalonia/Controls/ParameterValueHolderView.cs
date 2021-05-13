using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace WDE.Common.Avalonia.Controls
{
    public class ParameterValueHolderView : TemplatedControl
    {
        private ICommand? pickCommand;
        public static readonly DirectProperty<ParameterValueHolderView, ICommand?> PickCommandProperty = AvaloniaProperty.RegisterDirect<ParameterValueHolderView, ICommand?>("PickCommand", o => o.PickCommand, (o, v) => o.PickCommand = v);

        public ICommand? PickCommand
        {
            get => pickCommand;
            set => SetAndRaise(PickCommandProperty, ref pickCommand, value);
        }
    }
}