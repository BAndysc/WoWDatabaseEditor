using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.DbcStore.Views
{
    /// <summary>
    ///     Interaction logic for DBCConfigView.xaml
    /// </summary>
    public class DBCConfigView : UserControl
    {
        public DBCConfigView()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}