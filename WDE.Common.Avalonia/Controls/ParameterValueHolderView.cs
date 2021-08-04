using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls
{
    public class ParameterValueHolderView : TemplatedControl
    {
        private ICommand? pickCommand;
        public static readonly DirectProperty<ParameterValueHolderView, ICommand?> PickCommandProperty = AvaloniaProperty.RegisterDirect<ParameterValueHolderView, ICommand?>("PickCommand", o => o.PickCommand, (o, v) => o.PickCommand = v);
        
        private Func<Task<object?>>? specialCommand;
        public static readonly DirectProperty<ParameterValueHolderView, Func<Task<object?>>?> SpecialCommandProperty = AvaloniaProperty.RegisterDirect<ParameterValueHolderView, Func<Task<object?>>?>("SpecialCommand", o => o.SpecialCommand, (o, v) => o.SpecialCommand = v);

        public ICommand? PickCommand
        {
            get => pickCommand;
            set => SetAndRaise(PickCommandProperty, ref pickCommand, value);
        }
        
        public Func<Task<object?>>? SpecialCommand
        {
            get => specialCommand;
            set => SetAndRaise(SpecialCommandProperty, ref specialCommand, value);
        }
        
        public ICommand? PickSpecial { get; }

        public ParameterValueHolderView()
        {
            PickSpecial = new AsyncAutoCommand(async () =>
            {
                if (SpecialCommand == null)
                    return;

                var result = await SpecialCommand();

                if (result == null)
                    return;
                
                var tb = this.FindDescendantOfType<ParameterTextBox>();
                if (tb != null)
                    tb.Text = result.ToString();
            });
        }
    }
}