using System.Windows.Controls;

namespace WDE.SQLEditor.Views
{
    /// <summary>
    /// Interaction logic for SqlEditorView
    /// </summary>
    public partial class SqlEditorView : UserControl
    {
        public SqlEditorView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("WDE.SQLEditor.Resources.sql.xshd"))
            {
                using (var reader = new System.Xml.XmlTextReader(stream))
                {
                    MyAvalonEdit.SyntaxHighlighting =
                        ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader,
                        ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
                }
            }
        }
    }
}
