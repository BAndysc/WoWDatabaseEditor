using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using Prism.Commands;

namespace WDE.CommonViews.Avalonia.SQLEditor.Views
{
    /// <summary>
    ///     Interaction logic for SqlEditorView
    /// </summary>
    public partial class SqlEditorView : UserControl
    {
        public ICommand Undo { get; }
        public ICommand Redo { get; }
        public ICommand Paste { get; }
        public ICommand Cut { get; }
        public ICommand Copy { get; }

        private TextEditor? textEditor;
        
        public SqlEditorView()
        {
            Undo = new DelegateCommand(() => textEditor?.Undo());
            Redo = new DelegateCommand(() => textEditor?.Redo());
            Paste = new DelegateCommand(() => textEditor?.Paste());
            Cut = new DelegateCommand(() => textEditor?.Cut());
            Copy = new DelegateCommand(() => textEditor?.Copy());
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            textEditor = this.FindControl<TextEditor>("MyAvalonEdit");
        }
    }
}