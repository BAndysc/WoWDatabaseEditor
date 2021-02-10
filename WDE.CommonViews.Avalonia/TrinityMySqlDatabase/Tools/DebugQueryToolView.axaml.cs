using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;

namespace WDE.TrinityMySqlDatabase.Tools
{
    public class DebugQueryToolView : UserControl
    {
        private TextEditor MyAvalonEdit;
        
        public DebugQueryToolView()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            MyAvalonEdit = this.FindControl<TextEditor>("MyAvalonEdit");
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

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            
            if (DataContext is ICodeEditorViewModel codeEditorViewModel){
            
                codeEditorViewModel.Clear += CodeEditorViewModelOnClear;
                codeEditorViewModel.ScrollToEnd += CodeEditorViewModelOnScrollToEnd;
            }
        }
    }
}