using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.Common.WPF.Components
{
    public class TeachingTip : Control
    {
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(object), typeof(TeachingTip));

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(TeachingTip));

        public bool IsOpened
        {
            get => (bool)GetValue(IsOpenedProperty);
            set => SetValue(IsOpenedProperty, value);
        }

        public static readonly DependencyProperty IsOpenedProperty =
            DependencyProperty.Register("IsOpened", typeof(bool), typeof(TeachingTip));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public ICommand CloseCommand { get; }

        public TeachingTip()
        {
            CloseCommand = new DelegateCommand(() =>
            {
                IsOpened = false;
            });
        }

        static TeachingTip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TeachingTip), new FrameworkPropertyMetadata(typeof(TeachingTip)));
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