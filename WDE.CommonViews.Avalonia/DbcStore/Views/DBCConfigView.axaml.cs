using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CommonViews.Avalonia.DbcStore.Views
{
    /// <summary>
    ///     Interaction logic for DBCConfigView.xaml
    /// </summary>
    public partial class DBCConfigView : UserControl
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