using System;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
        private IDisposable? onDeactivated;

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
            onDeactivated?.Dispose();
            onDeactivated = null;
        }

        public void OnDeactivated()
        {
            onDeactivated?.Dispose();
            onDeactivated = null;
            onDeactivated = DispatcherTimer.RunOnce(() =>
            {
                if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime classic)
                {
                    var windows = classic.Windows;
                    var anyIsActive = windows.Any(x => x.IsActive);
                    Console.WriteLine("Any active: " + anyIsActive + " " + (anyIsActive ? "": "probably an alt-tab, ignoring"));
                    if (anyIsActive)
                    {
                        forceFocusFirstOnNextGotFocus = true;
                    }
                }
            }, TimeSpan.FromMilliseconds(16));
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (DataContext is ParametersEditViewModel vm)
            {
                vm.AttachView(this);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if (DataContext is ParametersEditViewModel vm)
            {
                vm.DetachView(this);
            }
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
