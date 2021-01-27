using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartEventView.xaml
    /// </summary>
    public class SmartEventView : Control
    {
        public static DependencyProperty EditEventCommandProperty =
            DependencyProperty.Register("EditEventCommand", typeof(ICommand), typeof(SmartEventView));

        public static DependencyProperty DeselectAllRequestProperty =
            DependencyProperty.Register(nameof(DeselectAllRequest), typeof(ICommand), typeof(SmartEventView));

        public static DependencyProperty DeselectActionsOfDeselectedEventsRequestProperty =
            DependencyProperty.Register(nameof(DeselectActionsOfDeselectedEventsRequest), typeof(ICommand), typeof(SmartEventView));

        public static DependencyProperty DirectEditParameterProperty =
            DependencyProperty.Register(nameof(DirectEditParameter), typeof(ICommand), typeof(SmartEventView));

        public static DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected),
            typeof(bool),
            typeof(SmartEventView),
            new PropertyMetadata(false));
        
        static SmartEventView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SmartEventView), new FrameworkPropertyMetadata(typeof(SmartEventView)));
        }

        public ICommand EditEventCommand
        {
            get => (ICommand) GetValue(EditEventCommandProperty);
            set => SetValue(EditEventCommandProperty, value);
        }

        public ICommand DeselectAllRequest
        {
            get => (ICommand) GetValue(DeselectAllRequestProperty);
            set => SetValue(DeselectAllRequestProperty, value);
        }

        public ICommand DeselectActionsOfDeselectedEventsRequest
        {
            get => (ICommand) GetValue(DeselectActionsOfDeselectedEventsRequestProperty);
            set => SetValue(DeselectActionsOfDeselectedEventsRequestProperty, value);
        }
        
        public ICommand DirectEditParameter
        {
            get => (ICommand) GetValue(DirectEditParameterProperty);
            set => SetValue(DirectEditParameterProperty, value);
        }

        public bool IsSelected
        {
            get => (bool) GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                if (DirectEditParameter != null)
                {
                    if (e.OriginalSource is Run originalRun && originalRun.DataContext != null &&
                        originalRun.DataContext != DataContext)
                    {
                        DirectEditParameter.Execute(originalRun.DataContext);
                        return;
                    }
                }

                DeselectActionsOfDeselectedEventsRequest?.Execute(null);

                if (!IsSelected)
                {
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
                e.Handled = true;
            }
            else if (e.ClickCount == 2)
            {
                EditEventCommand?.Execute(DataContext);
                e.Handled = true;
            }
            base.OnPreviewMouseDown(e);
        }
    }
}