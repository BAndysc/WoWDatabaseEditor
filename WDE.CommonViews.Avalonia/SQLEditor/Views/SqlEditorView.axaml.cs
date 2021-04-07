using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using Prism.Commands;

namespace WDE.SQLEditor.Views
{
    /// <summary>
    ///     Interaction logic for SqlEditorView
    /// </summary>
    public class SqlEditorView : UserControl
    {
        public static KeyGesture UndoGesture { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Undo.FirstOrDefault();

        public static KeyGesture RedoGesture { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Redo.FirstOrDefault();

        public ICommand Undo { get; }
        public ICommand Redo { get; }
        public ICommand Paste { get; }
        public ICommand Cut { get; }
        public ICommand Copy { get; }

        private TextEditor textEditor;
        
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