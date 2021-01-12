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
            MyAvalonEdit.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("TSQL");
        }
    }
}