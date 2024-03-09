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
    public partial class ParametersEditView : UserControl
    {
        public ParametersEditView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            DispatcherTimer.RunOnce(() =>
            {
                if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() == null)
                {
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
