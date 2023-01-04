using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace WoWDatabaseEditorCore.Avalonia.Views
{
    /// <summary>
    ///     Interaction logic for AboutView
    /// </summary>
    public partial class StatusBarView : UserControl
    {
        public StatusBarView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void HideFlyout(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var popup = btn.FindLogicalAncestorOfType<Popup>();
                if (popup != null)
                    popup.IsOpen = false;
            }
        }
    }
}