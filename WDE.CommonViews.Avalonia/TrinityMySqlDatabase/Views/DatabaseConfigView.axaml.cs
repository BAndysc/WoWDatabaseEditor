using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CommonViews.Avalonia.TrinityMySqlDatabase.Views
{
    /// <summary>
    ///     Interaction logic for DatabaseConfigView
    /// </summary>
    public partial class DatabaseConfigView : UserControl
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