using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using DynamicData;
using Markdown.Avalonia;
using Prism.Commands;
using WDE.Common.Avalonia.Controls.TextMarkers;
using WDE.Common.Utils;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Views;

public partial class SqlWorkbenchView : UserControl
{
    public SqlWorkbenchView()
    {
        CutCommand = new DelegateCommand(() => editor.Cut());
        CopyCommand = new DelegateCommand(() => editor.Copy());
        PasteCommand = new DelegateCommand(() => editor.Paste());
        DeleteCommand = new DelegateCommand(() => editor.Delete());
        SelectAllCommand = new DelegateCommand(() => editor.SelectAll());
        UndoCommand = new DelegateCommand(() => editor.Undo());
        RedoCommand = new DelegateCommand(() => editor.Redo());
        InitializeComponent();
    }
    
    private TextEditor editor = null!;
    
    private CompletionWindow _completionWindow = null!;

    public ICommand CutCommand { get; }
    
    public ICommand CopyCommand { get; }
    
    public ICommand PasteCommand { get; }
    
    public ICommand DeleteCommand { get; }
    
    public ICommand SelectAllCommand { get; }
    
    public ICommand UndoCommand { get; }
    
    public ICommand RedoCommand { get; }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        editor = this.GetControl<TextEditor>("TextEditor");
        editor.TextArea.Caret.PositionChanged += CaretPositionChanged;
        var commandBindings = editor.TextArea.DefaultInputHandler.Editing.CommandBindings;
        var deleteLineBinding = commandBindings.FirstOrDefault(b => b.Command.Gesture == new KeyGesture(Key.D, KeyModifiers.Control));
        if (deleteLineBinding != null)
            commandBindings.Remove(deleteLineBinding);
        commandBindings.Add(new RoutedCommandBinding(new RoutedCommand("Duplicate line", new KeyGesture(Key.D, KeyModifiers.Control)), (sender, args) =>
        {
            var document = editor.Document;
            var caret = editor.TextArea.Caret;
            var oldColumn = caret.Column;
            var line = document.GetLineByNumber(caret.Line);
            var lineText = document.GetText(line.Offset, line.Length);
            document.Insert(line.Offset, lineText + Environment.NewLine);
            caret.Offset = Math.Min(line.EndOffset + oldColumn, document.TextLength);
        }));
        
        editor.Options.AllowScrollBelowDocument = false;
        _completionWindow = new CompletionWindow(editor.TextArea);
        _completionWindow.Styles.Add(new StyleInclude(new Uri("resm:Styles?assembly=WDE.SqlWorkbench")){Source = new Uri("avares://WDE.SqlWorkbench/Generic.axaml")});
        _completionWindow.WindowManagerAddShadowHint = true;
        _completionWindow.Width = 450;
        _completionWindow.MaxHeight = 600;
        _completionWindow.IsLightDismissEnabled = true;
        _completionWindow.CloseAutomatically = false;
        _completionWindow.CloseWhenCaretAtBeginning = false;

        BindToDataContext();
    }

    private SqlWorkbenchViewModel? boundVm;
    
    private void BindToDataContext()
    {
        UnbindFromDataContext();
        if (editor == null)
            return;
        
        if (DataContext is SqlWorkbenchViewModel vm)
        {
            boundVm = vm;
            boundVm.IsViewBound = true;
            vm.Completions.CollectionChanged += OnCompletionsSetChanged;
            vm.PropertyChanged += OnViewModelPropertyChanged;
            editor.Document = vm.Document; // <-- this is required, because when we add the text marker service below, the document must be set already!
            editor.TextArea.TextView.BackgroundRenderers.Add(vm.TextMarkerService);
            editor.TextArea.TextView.LineTransformers.Add(vm.TextMarkerService);
            editor.TextArea.Options.ShowSpaces = editor.TextArea.Options.ShowTabs = editor.TextArea.Options.ShowEndOfLine = vm.ShowNonPrintableChars;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SqlWorkbenchViewModel.ShowNonPrintableChars))
            editor.TextArea.Options.ShowSpaces = editor.TextArea.Options.ShowTabs = editor.TextArea.Options.ShowEndOfLine = boundVm!.ShowNonPrintableChars;
        else if (e.PropertyName == nameof(SqlWorkbenchViewModel.HasAnyResults))
        {
            var grid = this.GetControl<Grid>("Grid");
            if (boundVm!.HasAnyResults)
            {
                if (grid.RowDefinitions[2].ActualHeight == 0)
                    grid.RowDefinitions = new RowDefinitions("*,5,*");
            }
            else
                grid.RowDefinitions = new RowDefinitions("*,5,0");
        }
    }

    private void UnbindFromDataContext()
    {
        if (boundVm == null)
            return;

        boundVm.IsViewBound = false;
        boundVm.Completions.CollectionChanged -= OnCompletionsSetChanged;
        boundVm.PropertyChanged -= OnViewModelPropertyChanged;
        editor.TextArea.TextView.BackgroundRenderers.Remove(boundVm.TextMarkerService);
        editor.TextArea.TextView.LineTransformers.Remove(boundVm.TextMarkerService);
        boundVm = null;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        BindToDataContext();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        BindToDataContext();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnbindFromDataContext();
    }

    private void OnCompletionsSetChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is SqlWorkbenchViewModel vm)
        {
            if (vm.Completions.Count > 0)
            {
                if (!_completionWindow.IsOpen)
                {
                    _completionWindow.CompletionList.CompletionData.Clear();
                    _completionWindow.CompletionList.CompletionData.AddRange(vm.Completions);
                    _completionWindow.Show();
                    _completionWindow.RefreshCompletion();
                }
            }
            else
            {
                _completionWindow.Close();
            }
        }
    }

    private void CaretPositionChanged(object? sender, EventArgs e)
    {
        if (DataContext is SqlWorkbenchViewModel vm)
        {
            vm.CaretOffset = editor.CaretOffset;
            vm.CaretLine = editor.TextArea.Caret.Line;
            vm.CaretColumn = editor.TextArea.Caret.Column;
        }
    }
    
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key is Key.Up or Key.Down && _completionWindow.IsOpen)
            return;

        if (e.Key is Key.Left or Key.Right && _completionWindow.IsOpen)
        {
            _completionWindow.Close();
        }
    }
    
    private void TextEditor_OnPointerHover(object? sender, PointerEventArgs e)
    {
        var position = editor.GetPositionFromPoint(e.GetPosition(editor));
        if (position.HasValue && DataContext is SqlWorkbenchViewModel vm)
        {
            var offset = editor.Document.GetOffset(position.Value.Location);
            var marker = vm.TextMarkerService.GetMarkersAtOffset(TextMarkerTypes.SquigglyUnderline, offset).FirstOrDefault();
            if (marker != null && marker.ToolTip != null)
            {
                editor.SetValue(ToolTip.TipProperty, (string)marker.ToolTip);
                editor.SetValue(ToolTip.IsOpenProperty, true);
            }
            ShowHoverInfoIfAnyAsync(position.Value).ListenErrors();
        }
    }

    private async Task ShowHoverInfoIfAnyAsync(TextViewPosition textViewPosition)
    {
        if (DataContext is SqlWorkbenchViewModel vm)
        {
            var text = await vm.GetHoverInfoAsync(textViewPosition.Line, textViewPosition.Column);
            if (text != null)
            {
                var md = this.GetControl<MarkdownScrollViewer>("MarkdownHoverViewer");
                md.Markdown = text;
                
                var flyout = FlyoutBase.GetAttachedFlyout(editor);
                (flyout as Flyout)?.ShowAt(editor, true);
            }
        }
    }

    private void TextEditor_OnPointerHoverStopped(object? sender, PointerEventArgs e)
    {
        editor.SetValue(ToolTip.IsOpenProperty, false);
        editor.ClearValue(ToolTip.TipProperty);
    }

    private void TextEditor_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && e.KeyModifiers.HasFlagFast(KeyModifiers.Control))
        {
            if (DataContext is SqlWorkbenchViewModel vm)
            {
                vm.ComputeCompletionAsync(true).ListenErrors();
                e.Handled = true;
            }
        }
    }
}