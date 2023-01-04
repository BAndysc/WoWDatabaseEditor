using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Avalonia.Views;

public partial class RemoteConnectionToolView : UserControl
{
    public RemoteConnectionToolView()
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
            
        if (DataContext is RemoteConnectionToolViewModel vm){
            
            vm.Clear += CodeEditorViewModelOnClear;
            vm.ScrollToEnd += CodeEditorViewModelOnScrollToEnd;
        }
    }
}