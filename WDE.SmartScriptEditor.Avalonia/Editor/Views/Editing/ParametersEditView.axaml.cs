using System;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WDE.Common.Avalonia.Utils;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views.Editing
{
    /// <summary>
    ///     Interaction logic for ParametersEditView.xaml
    /// </summary>
    public partial class ParametersEditView : UserControl, IDialogWindowActivatedListener
    {
        private bool forceFocusFirstOnNextGotFocus = false;

        public ParametersEditView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnActivated()
        {
        }

        public void OnDeactivated()
        {
            forceFocusFirstOnNextGotFocus = true;
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            DispatcherTimer.RunOnce(() =>
            {
                if (!this.FindAncestorOfType<Window>()?.IsActive ?? true)
                    return;
                var currentFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
                if (currentFocus == null || ReferenceEquals(currentFocus, this) || forceFocusFirstOnNextGotFocus)
                {
                    forceFocusFirstOnNextGotFocus = false;
                    if (FindFirstParameter(this) is { } firstParam)
                        FocusUtils.FocusFirstFocusableChild(firstParam);
                }
            }, TimeSpan.FromMilliseconds(1));
        }
        
        private static ParameterEditorView? FindFirstParameter(ILogical that)
        {
            IAvaloniaReadOnlyList<ILogical> logicalChildren = that.LogicalChildren;
            int count = logicalChildren.Count;
            for (int index = 0; index < count; ++index)
            {
                ILogical logical = logicalChildren[index];
                if (logical is ParameterEditorView view && view.DataContext is EditableParameterViewModel<long> context && context.IsFirstParameter)
                    return view;
                
                ParameterEditorView? find = FindFirstParameter(logical);
                if (find != null)
                    return find;
            }
            return null;
        }
    }
}
