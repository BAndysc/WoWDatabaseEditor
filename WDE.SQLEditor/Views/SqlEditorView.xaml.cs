using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace WDE.SQLEditor.Views
{
    /// <summary>
    ///     Interaction logic for SqlEditorView
    /// </summary>
    public partial class SqlEditorView : UserControl
    {
        public SqlEditorView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WDE.SQLEditor.Resources.sql.xshd"))
            {
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    MyAvalonEdit.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
        }
    }
}