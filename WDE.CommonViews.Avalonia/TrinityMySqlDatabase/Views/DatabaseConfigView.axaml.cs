
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.TrinityMySqlDatabase.Views
{
    /// <summary>
    ///     Interaction logic for DatabaseConfigView
    /// </summary>
    public class DatabaseConfigView : UserControl
    {
        public DatabaseConfigView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}