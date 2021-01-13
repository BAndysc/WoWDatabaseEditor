using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.SmartScriptEditor.Editor.UserControls
{
    /// <summary>
    ///     Interaction logic for SmartActionView.xaml
    /// </summary>
    public partial class SmartConditionView : UserControl
    {
        public static DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected),
            typeof(bool),
            typeof(SmartConditionView),
            new PropertyMetadata(false));

        public static DependencyProperty DeselectAllRequestProperty =
            DependencyProperty.Register(nameof(DeselectAllRequest), typeof(ICommand), typeof(SmartConditionView));

        public static DependencyProperty DeselectAllButConditionsRequestProperty =
            DependencyProperty.Register(nameof(DeselectAllButConditionsRequest), typeof(ICommand), typeof(SmartConditionView));

        public static DependencyProperty EditConditionCommandProperty =
            DependencyProperty.Register(nameof(EditConditionCommand), typeof(ICommand), typeof(SmartConditionView));

        public SmartConditionView()
        {
            InitializeComponent();
        }

        public bool IsSelected
        {
            get => (bool) GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public ICommand DeselectAllRequest
        {
            get => (ICommand) GetValue(DeselectAllRequestProperty);
            set => SetValue(DeselectAllRequestProperty, value);
        }

        public ICommand DeselectAllButConditionsRequest
        {
            get => (ICommand) GetValue(DeselectAllButConditionsRequestProperty);
            set => SetValue(DeselectAllButConditionsRequestProperty, value);
        }

        public ICommand EditConditionCommand
        {
            get => (ICommand) GetValue(EditConditionCommandProperty);
            set => SetValue(EditConditionCommandProperty, value);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ClickCount == 1)
            {
                DeselectAllButConditionsRequest?.Execute(null);
                if (!IsSelected)
                {
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                        DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
            }
            else if (e.ClickCount == 2)
                EditConditionCommand?.Execute(DataContext);
        }
    }
}