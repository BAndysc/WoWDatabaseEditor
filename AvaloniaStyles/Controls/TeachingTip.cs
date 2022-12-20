using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Metadata;

namespace AvaloniaStyles.Controls
{
    public class TeachingTip : TemplatedControl
    {
        public static readonly StyledProperty<object> ContentProperty =
            AvaloniaProperty.Register<TeachingTip, object>(nameof(Content));
        
        [Content]
        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }
        
        public static readonly StyledProperty<string> HeaderProperty =
            AvaloniaProperty.Register<TeachingTip, string>(nameof(Header));

        private bool isOpened;
        public static readonly DirectProperty<TeachingTip, bool> IsOpenedProperty = 
            AvaloniaProperty.RegisterDirect<TeachingTip, bool>("IsOpened", o => o.IsOpened, (o, v) => o.IsOpened = v, defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> IsDontWorryHintVisibleProperty = AvaloniaProperty.Register<TeachingTip, bool>("IsDontWorryHintVisible", true);

        public string Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public bool IsOpened
        {
            get => isOpened;
            set
            {
                SetAndRaise(IsOpenedProperty, ref isOpened, value);
                IsVisible = value;
            }
        }
        
        public ICommand CloseCommand { get; }

        public bool IsDontWorryHintVisible
        {
            get => (bool)GetValue(IsDontWorryHintVisibleProperty);
            set => SetValue(IsDontWorryHintVisibleProperty, value);
        }

        public TeachingTip()
        {
            CloseCommand = new DelegateCommand(() =>
            {
                IsOpened = false;
            });
        }
    }

    internal class DelegateCommand : ICommand
    {
        private readonly Action action;

        public DelegateCommand(System.Action action)
        {
            this.action = action;
        }
        
        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            action?.Invoke();
        }

        public event EventHandler? CanExecuteChanged;
    }
}