using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using WDE.Common.Avalonia.Components;
using WDE.MySqlDatabaseCommon.Tools;

namespace WDE.CommonViews.Avalonia.TrinityMySqlDatabase.Tools
{
    public partial class DebugQueryToolView : ToolView
    {
        public DebugQueryToolView()
        {
            InitializeComponent();
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