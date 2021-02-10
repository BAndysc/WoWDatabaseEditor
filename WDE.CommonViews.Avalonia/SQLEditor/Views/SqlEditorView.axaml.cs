using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit.Highlighting;

namespace WDE.SQLEditor.Views
{
    /// <summary>
    ///     Interaction logic for SqlEditorView
    /// </summary>
    public class SqlEditorView : UserControl
    {
        public SqlEditorView()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}