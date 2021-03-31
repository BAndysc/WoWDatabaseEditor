using System.Windows;
using System.Windows.Controls;
using WDE.MySqlDatabaseCommon.Tools;

namespace WDE.TrinityMySqlDatabase.Tools
{
    public partial class DebugQueryToolView : UserControl
    {
        public DebugQueryToolView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ICodeEditorViewModel codeEditorViewModel)
            {
                codeEditorViewModel.Clear -= CodeEditorViewModelOnClear;
                codeEditorViewModel.ScrollToEnd -= CodeEditorViewModelOnScrollToEnd;        
            }    
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ICodeEditorViewModel codeEditorViewModel){
            
                codeEditorViewModel.Clear += CodeEditorViewModelOnClear;
                codeEditorViewModel.ScrollToEnd += CodeEditorViewModelOnScrollToEnd;
            }
        }
        
        private void CodeEditorViewModelOnClear()
        {
            if (MyAvalonEdit?.Document == null || MyAvalonEdit?.TextArea == null)
                return;
            
            MyAvalonEdit.Clear();
        }

        private void CodeEditorViewModelOnScrollToEnd()
        {
            if (MyAvalonEdit?.Document == null || MyAvalonEdit?.TextArea == null)
                return;
            
            MyAvalonEdit.CaretOffset = MyAvalonEdit.Document.TextLength;
            MyAvalonEdit.TextArea.Caret.BringCaretToView();
        }
    }
}